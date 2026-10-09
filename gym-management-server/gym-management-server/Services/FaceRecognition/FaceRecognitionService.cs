using gym_management_server.DTOs.FaceRecognition;
using gym_management_server.Entities.CheckIns;
using gym_management_server.Entities.Enums;
using gym_management_server.Entities.FaceRecognition;
using gym_management_server.FaceRecognition;
using gym_management_server.Fingerprints;
using gym_management_server.Infrastructure.Tenancy;
using gym_management_server.Repositories.CheckIns;
using gym_management_server.Repositories.Devices;
using gym_management_server.Repositories.FaceRecognition;
using gym_management_server.Repositories.Members;
using Microsoft.EntityFrameworkCore;

namespace gym_management_server.Services.FaceRecognition
{
    /// <summary>
    /// Orchestrates face enrolment and the verify -> check-in/check-out flow. Deliberately mirrors
    /// Services/Fingerprints/FingerprintService.cs line for line where the logic is modality-
    /// independent (tenant resolution from the scanning device, the check-in/check-out toggle and
    /// its race-condition handling) - see docs/ImprovementPlan.md mục 5 for why this is a parallel
    /// pipeline rather than a shared one. Generic attendance-history/dashboard aggregates are NOT
    /// duplicated here: they don't care which modality produced a CheckIn, so FingerprintService's
    /// versions already serve both.
    /// </summary>
    public class FaceRecognitionService
    {
        private readonly IFaceTemplateRepository _templates;
        private readonly IAttendanceDeviceRepository _devices;
        private readonly ICheckInRepository _checkIns;
        private readonly IMemberRepository _members;
        private readonly IFaceRecognitionProviderFactory _providerFactory;
        private readonly ITemplateProtector _protector;
        private readonly GymManagementServiceMapObjects _mapObjects;
        private readonly ICurrentTenantAccessor _currentTenant;

        public FaceRecognitionService(
            IFaceTemplateRepository templates,
            IAttendanceDeviceRepository devices,
            ICheckInRepository checkIns,
            IMemberRepository members,
            IFaceRecognitionProviderFactory providerFactory,
            ITemplateProtector protector,
            GymManagementServiceMapObjects mapObjects,
            ICurrentTenantAccessor currentTenant)
        {
            _templates = templates;
            _devices = devices;
            _checkIns = checkIns;
            _members = members;
            _providerFactory = providerFactory;
            _protector = protector;
            _mapObjects = mapObjects;
            _currentTenant = currentTenant;
        }

        // ---------- Enrolment ----------

        public async Task<List<FaceTemplateOutput>> GetByMemberAsync(Guid memberId)
        {
            var list = await _templates.GetByMemberAsync(memberId);
            return list.Select(t => _mapObjects.MapObjects<FaceTemplate, FaceTemplateOutput>(t)).ToList();
        }

        public async Task<FaceTemplateOutput> RegisterAsync(FaceTemplateInput input, Guid? createdBy)
        {
            var raw = DecodeTemplate(input.CapturedTemplate);

            // Let the vendor provider validate/normalise before we encrypt and persist.
            var provider = _providerFactory.GetProvider(input.Vendor);
            var normalised = provider.CreateTemplate(raw);

            var entity = new FaceTemplate
            {
                Id = Guid.NewGuid(),
                MemberId = input.MemberId,
                Vendor = input.Vendor,
                Quality = input.Quality,
                Template = _protector.Protect(normalised),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy
            };

            await _templates.AddAsync(entity);
            return _mapObjects.MapObjects<FaceTemplate, FaceTemplateOutput>(entity);
        }

        public async Task<FaceTemplateOutput?> UpdateAsync(Guid id, FaceTemplateInput input)
        {
            var entity = await _templates.GetByIdAsync(id);
            if (entity == null) return null;

            var raw = DecodeTemplate(input.CapturedTemplate);
            var provider = _providerFactory.GetProvider(input.Vendor);
            var normalised = provider.CreateTemplate(raw);

            entity.Vendor = input.Vendor;
            entity.Quality = input.Quality;
            entity.Template = _protector.Protect(normalised);
            entity.UpdatedAt = DateTime.UtcNow;

            await _templates.UpdateAsync(entity);
            return _mapObjects.MapObjects<FaceTemplate, FaceTemplateOutput>(entity);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _templates.GetByIdAsync(id);
            if (entity == null) return false;
            await _templates.DeleteAsync(id);
            return true;
        }

        // ---------- Verify -> check-in / check-out ----------

        public async Task<VerifyFaceResult> VerifyAsync(VerifyFaceInput input)
        {
            // Anonymous endpoint (shared device API key, not a user JWT - see
            // FaceRecognitionController), so there is no ambient tenant yet. The device itself
            // determines it: look it up ignoring the tenant filter, then set the ambient tenant
            // from what it belongs to, so every query below (templates, member, check-ins) is
            // correctly scoped - a device/member can never match across tenants.
            var device = await _devices.GetByIdIgnoringTenantAsync(input.DeviceId)
                ?? throw new InvalidOperationException($"Device '{input.DeviceId}' was not found.");
            if (!device.IsActive)
                throw new InvalidOperationException($"Device '{device.Name}' is inactive.");
            _currentTenant.SetTenant(device.TenantId);

            var probe = DecodeTemplate(input.CapturedTemplate);
            var provider = _providerFactory.GetProvider(device.Vendor);

            // Build candidate set by decrypting stored templates in memory only.
            var stored = await _templates.GetAllActiveAsync();
            var candidates = stored
                .Select(t => new FaceCandidate(t.MemberId, t.Id, _protector.Unprotect(t.Template)))
                .ToList();

            var match = provider.Identify(probe, candidates);

            if (!match.Matched || match.MemberId is null)
                return new VerifyFaceResult { Matched = false, Score = match.Score, Action = "none" };

            var memberId = match.MemberId.Value;
            var member = await _members.GetByIdAsync(memberId);
            var now = DateTime.UtcNow;

            // Toggle attendance: open a session if none active, otherwise close the active one.
            var active = await _checkIns.GetActiveByMemberAsync(memberId);
            if (active == null)
            {
                var session = new CheckIn
                {
                    Id = Guid.NewGuid(),
                    MemberId = memberId,
                    CheckInTime = now,
                    Method = CheckInMethod.Face,
                    DeviceId = device.Id,
                    OperatorUserId = input.OperatorUserId
                };

                try
                {
                    await _checkIns.AddAsync(session);

                    return new VerifyFaceResult
                    {
                        Matched = true,
                        Score = match.Score,
                        MemberId = memberId,
                        MemberName = member?.FullName,
                        Action = "check-in",
                        CheckInId = session.Id,
                        Timestamp = now
                    };
                }
                catch (DbUpdateException)
                {
                    // Lost a race: another scan/request opened a session for this member between
                    // our read above and this insert. The unique index on CheckIns(MemberId)
                    // WHERE CheckOutTime IS NULL rejected the duplicate. Fall back to treating this
                    // scan as the check-out of the session the other request just opened, instead
                    // of surfacing a 500 or silently creating a second open session.
                    active = await _checkIns.GetActiveByMemberAsync(memberId);
                    if (active == null)
                        throw; // not the expected race - some other failure.
                }
            }

            active.CheckOutTime = now;
            active.CheckOutMethod = CheckOutMethod.Scan;
            await _checkIns.UpdateAsync(active);

            return new VerifyFaceResult
            {
                Matched = true,
                Score = match.Score,
                MemberId = memberId,
                MemberName = member?.FullName,
                Action = "check-out",
                CheckInId = active.Id,
                Timestamp = now
            };
        }

        private static byte[] DecodeTemplate(string base64)
        {
            if (string.IsNullOrWhiteSpace(base64))
                throw new ArgumentException("Captured template is required.", nameof(base64));
            try
            {
                return Convert.FromBase64String(base64);
            }
            catch (FormatException)
            {
                throw new ArgumentException("Captured template must be valid base64.", nameof(base64));
            }
        }
    }
}
