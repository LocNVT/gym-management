using gym_management_server.DTOs.CheckIns;
using gym_management_server.DTOs.Common;
using gym_management_server.DTOs.Fingerprints;
using gym_management_server.Entities.CheckIns;
using gym_management_server.Entities.Enums;
using gym_management_server.Entities.Fingerprints;
using gym_management_server.Fingerprints;
using gym_management_server.Repositories.CheckIns;
using gym_management_server.Repositories.Devices;
using gym_management_server.Repositories.Fingerprints;
using gym_management_server.Repositories.Members;

namespace gym_management_server.Services.Fingerprints
{
    /// <summary>
    /// Orchestrates fingerprint enrolment and the verify -> check-in/check-out flow.
    /// Templates are encrypted on the way in and only decrypted transiently in memory for matching.
    /// </summary>
    public class FingerprintService
    {
        private readonly IFingerprintTemplateRepository _templates;
        private readonly IAttendanceDeviceRepository _devices;
        private readonly ICheckInRepository _checkIns;
        private readonly IMemberRepository _members;
        private readonly IFingerprintProviderFactory _providerFactory;
        private readonly ITemplateProtector _protector;
        private readonly GymManagementServiceMapObjects _mapObjects;

        public FingerprintService(
            IFingerprintTemplateRepository templates,
            IAttendanceDeviceRepository devices,
            ICheckInRepository checkIns,
            IMemberRepository members,
            IFingerprintProviderFactory providerFactory,
            ITemplateProtector protector,
            GymManagementServiceMapObjects mapObjects)
        {
            _templates = templates;
            _devices = devices;
            _checkIns = checkIns;
            _members = members;
            _providerFactory = providerFactory;
            _protector = protector;
            _mapObjects = mapObjects;
        }

        // ---------- Enrolment ----------

        public async Task<List<FingerprintTemplateOutput>> GetByMemberAsync(Guid memberId)
        {
            var list = await _templates.GetByMemberAsync(memberId);
            return list.Select(t => _mapObjects.MapObjects<FingerprintTemplate, FingerprintTemplateOutput>(t)).ToList();
        }

        public async Task<FingerprintTemplateOutput> RegisterAsync(FingerprintTemplateInput input, Guid? createdBy)
        {
            var raw = DecodeTemplate(input.CapturedTemplate);

            // Let the vendor provider validate/normalise before we encrypt and persist.
            var provider = _providerFactory.GetProvider(input.Vendor);
            var normalised = provider.CreateTemplate(raw);

            var entity = new FingerprintTemplate
            {
                Id = Guid.NewGuid(),
                MemberId = input.MemberId,
                FingerPosition = input.FingerPosition,
                Vendor = input.Vendor,
                Quality = input.Quality,
                Template = _protector.Protect(normalised),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy
            };

            await _templates.AddAsync(entity);
            return _mapObjects.MapObjects<FingerprintTemplate, FingerprintTemplateOutput>(entity);
        }

        public async Task<FingerprintTemplateOutput?> UpdateAsync(Guid id, FingerprintTemplateInput input)
        {
            var entity = await _templates.GetByIdAsync(id);
            if (entity == null) return null;

            var raw = DecodeTemplate(input.CapturedTemplate);
            var provider = _providerFactory.GetProvider(input.Vendor);
            var normalised = provider.CreateTemplate(raw);

            entity.FingerPosition = input.FingerPosition;
            entity.Vendor = input.Vendor;
            entity.Quality = input.Quality;
            entity.Template = _protector.Protect(normalised);
            entity.UpdatedAt = DateTime.UtcNow;

            await _templates.UpdateAsync(entity);
            return _mapObjects.MapObjects<FingerprintTemplate, FingerprintTemplateOutput>(entity);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _templates.GetByIdAsync(id);
            if (entity == null) return false;
            await _templates.DeleteAsync(id);
            return true;
        }

        // ---------- Verify -> check-in / check-out ----------

        public async Task<VerifyFingerprintResult> VerifyAsync(VerifyFingerprintInput input)
        {
            var device = await _devices.GetByIdAsync(input.DeviceId)
                ?? throw new InvalidOperationException($"Device '{input.DeviceId}' was not found.");
            if (!device.IsActive)
                throw new InvalidOperationException($"Device '{device.Name}' is inactive.");

            var probe = DecodeTemplate(input.CapturedTemplate);
            var provider = _providerFactory.GetProvider(device.Vendor);

            // Build candidate set by decrypting stored templates in memory only.
            var stored = await _templates.GetAllActiveAsync();
            var candidates = stored
                .Select(t => new FingerprintCandidate(t.MemberId, t.Id, _protector.Unprotect(t.Template)))
                .ToList();

            var match = provider.Identify(probe, candidates);

            if (!match.Matched || match.MemberId is null)
                return new VerifyFingerprintResult { Matched = false, Score = match.Score, Action = "none" };

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
                    Method = CheckInMethod.Fingerprint,
                    DeviceId = device.Id,
                    OperatorUserId = input.OperatorUserId
                };
                await _checkIns.AddAsync(session);

                return new VerifyFingerprintResult
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

            active.CheckOutTime = now;
            await _checkIns.UpdateAsync(active);

            return new VerifyFingerprintResult
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

        // ---------- Attendance history ----------

        public async Task<PagedResult<AttendanceOutput>> GetAttendanceHistoryAsync(Guid memberId, int page = 1, int pageSize = 10)
        {
            var (items, totalCount) = await _checkIns.GetByMemberPagedAsync(memberId, page, pageSize);
            var mapped = items.Select(c => _mapObjects.MapObjects<CheckIn, AttendanceOutput>(c)).ToList();
            return new PagedResult<AttendanceOutput> { Items = mapped, TotalCount = totalCount, Page = page, PageSize = pageSize };
        }

        // ---------- Dashboard aggregates ----------

        public async Task<List<ActiveAttendanceOutput>> GetActiveAttendanceAsync()
        {
            var now = DateTime.UtcNow;
            var sessions = await _checkIns.GetActiveSessionsAsync();
            return sessions.Select(s => new ActiveAttendanceOutput
            {
                CheckInId = s.Id,
                MemberId = s.MemberId,
                MemberName = s.Member?.FullName,
                CheckInTime = s.CheckInTime,
                MinutesInside = (int)Math.Max(0, (now - s.CheckInTime).TotalMinutes),
                DeviceId = s.DeviceId
            }).ToList();
        }

        public async Task<AttendanceSummaryOutput> GetSummaryAsync(DateTime? date = null)
        {
            // Operate in UTC to match stored timestamps.
            var day = (date ?? DateTime.UtcNow).Date;
            var from = DateTime.SpecifyKind(day, DateTimeKind.Utc);
            var to = from.AddDays(1);

            return new AttendanceSummaryOutput
            {
                Date = from,
                CheckInsToday = await _checkIns.CountCheckInsBetweenAsync(from, to),
                CheckOutsToday = await _checkIns.CountCheckOutsBetweenAsync(from, to),
                CurrentlyInside = await _checkIns.CountCurrentlyInsideAsync()
            };
        }

        public async Task<List<AttendanceEventOutput>> GetRecentEventsAsync(int count = 20)
        {
            if (count <= 0) count = 20;
            var sessions = await _checkIns.GetRecentAsync(count);
            return sessions.Select(s => new AttendanceEventOutput
            {
                CheckInId = s.Id,
                MemberId = s.MemberId,
                MemberName = s.Member?.FullName,
                Action = s.CheckOutTime != null ? "check-out" : "check-in",
                Timestamp = s.CheckOutTime ?? s.CheckInTime
            }).ToList();
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
