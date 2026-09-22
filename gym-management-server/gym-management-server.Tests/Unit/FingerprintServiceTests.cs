using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using gym_management_server.Data.EntityFramework;
using gym_management_server.DTOs.Fingerprints;
using gym_management_server.Entities.CheckIns;
using gym_management_server.Entities.Devices;
using gym_management_server.Entities.Enums;
using gym_management_server.Entities.Members;
using gym_management_server.Fingerprints;
using gym_management_server.Fingerprints.Providers;
using gym_management_server.Repositories.CheckIns;
using gym_management_server.Repositories.Devices;
using gym_management_server.Repositories.Fingerprints;
using gym_management_server.Repositories.Members;
using gym_management_server.Services;
using gym_management_server.Services.Fingerprints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace gym_management_server.Tests.Unit
{
    public class FingerprintServiceTests
    {
        private static GymManagementContext NewContext()
        {
            var options = new DbContextOptionsBuilder<GymManagementContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new GymManagementContext(options);
        }

        private static FingerprintService BuildService(GymManagementContext db, out Guid memberId, out Guid deviceId)
        {
            var member = new Member { Id = Guid.NewGuid(), FullName = "Test Member", PhoneNumber = "0900000000" };
            var device = new AttendanceDevice { Id = Guid.NewGuid(), Name = "Front Door", Vendor = "Mock", IsActive = true };
            db.Members.Add(member);
            db.AttendanceDevices.Add(device);
            db.SaveChanges();

            memberId = member.Id;
            deviceId = device.Id;

            var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Fingerprint:EncryptionKey"] = key })
                .Build();

            var factory = new FingerprintProviderFactory(new IFingerprintProvider[] { new MockFingerprintProvider() });

            return new FingerprintService(
                new FingerprintTemplateRepository(db),
                new AttendanceDeviceRepository(db),
                new CheckInRepository(db),
                new MemberRepository(db),
                factory,
                new AesTemplateProtector(config),
                new GymManagementServiceMapObjects());
        }

        private static string Template(string seed)
        {
            // 32 deterministic bytes derived from the seed.
            var bytes = new byte[32];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = (byte)((seed.GetHashCode() >> (i % 24)) ^ (seed[i % seed.Length] + i));
            return Convert.ToBase64String(bytes);
        }

        [Fact]
        public async Task Register_stores_encrypted_template_and_omits_bytes_in_output()
        {
            using var db = NewContext();
            var service = BuildService(db, out var memberId, out _);

            var output = await service.RegisterAsync(new FingerprintTemplateInput
            {
                MemberId = memberId,
                FingerPosition = 1,
                CapturedTemplate = Template("alice"),
                Vendor = "Mock",
                Quality = 88
            }, createdBy: null);

            Assert.Equal(memberId, output.MemberId);
            Assert.Equal((byte)88, output.Quality);

            var stored = await db.FingerprintTemplates.SingleAsync();
            // Stored bytes must not equal the raw captured template (they are encrypted).
            Assert.NotEqual(Convert.FromBase64String(Template("alice")), stored.Template);
        }

        [Fact]
        public async Task Verify_creates_checkin_when_no_active_session()
        {
            using var db = NewContext();
            var service = BuildService(db, out var memberId, out var deviceId);

            await service.RegisterAsync(new FingerprintTemplateInput
            {
                MemberId = memberId, FingerPosition = 0, CapturedTemplate = Template("bob"), Vendor = "Mock"
            }, null);

            var result = await service.VerifyAsync(new VerifyFingerprintInput
            {
                DeviceId = deviceId, CapturedTemplate = Template("bob")
            });

            Assert.True(result.Matched);
            Assert.Equal("check-in", result.Action);
            Assert.Equal(memberId, result.MemberId);

            var session = await db.CheckIns.SingleAsync();
            Assert.Null(session.CheckOutTime);
            Assert.Equal(CheckInMethod.Fingerprint, session.Method);
            Assert.Equal(deviceId, session.DeviceId);
        }

        [Fact]
        public async Task Verify_checks_out_when_active_session_exists()
        {
            using var db = NewContext();
            var service = BuildService(db, out var memberId, out var deviceId);

            await service.RegisterAsync(new FingerprintTemplateInput
            {
                MemberId = memberId, FingerPosition = 0, CapturedTemplate = Template("carol"), Vendor = "Mock"
            }, null);

            var first = await service.VerifyAsync(new VerifyFingerprintInput { DeviceId = deviceId, CapturedTemplate = Template("carol") });
            var second = await service.VerifyAsync(new VerifyFingerprintInput { DeviceId = deviceId, CapturedTemplate = Template("carol") });

            Assert.Equal("check-in", first.Action);
            Assert.Equal("check-out", second.Action);
            Assert.Equal(first.CheckInId, second.CheckInId); // same session closed

            var session = await db.CheckIns.SingleAsync();
            Assert.NotNull(session.CheckOutTime);
        }

        [Fact]
        public async Task Verify_toggles_back_to_checkin_on_third_scan()
        {
            using var db = NewContext();
            var service = BuildService(db, out var memberId, out var deviceId);

            await service.RegisterAsync(new FingerprintTemplateInput
            {
                MemberId = memberId, FingerPosition = 0, CapturedTemplate = Template("dave"), Vendor = "Mock"
            }, null);

            await service.VerifyAsync(new VerifyFingerprintInput { DeviceId = deviceId, CapturedTemplate = Template("dave") });
            await service.VerifyAsync(new VerifyFingerprintInput { DeviceId = deviceId, CapturedTemplate = Template("dave") });
            var third = await service.VerifyAsync(new VerifyFingerprintInput { DeviceId = deviceId, CapturedTemplate = Template("dave") });

            Assert.Equal("check-in", third.Action);
            Assert.Equal(2, await db.CheckIns.CountAsync()); // two distinct sessions
        }

        [Fact]
        public async Task Verify_returns_no_match_for_unknown_finger()
        {
            using var db = NewContext();
            var service = BuildService(db, out var memberId, out var deviceId);

            await service.RegisterAsync(new FingerprintTemplateInput
            {
                MemberId = memberId, FingerPosition = 0, CapturedTemplate = Template("known"), Vendor = "Mock"
            }, null);

            var result = await service.VerifyAsync(new VerifyFingerprintInput
            {
                DeviceId = deviceId, CapturedTemplate = Template("totally-different-finger-xyz")
            });

            Assert.False(result.Matched);
            Assert.Equal("none", result.Action);
            Assert.Equal(0, await db.CheckIns.CountAsync());
        }

        [Fact]
        public async Task Verify_throws_for_inactive_device()
        {
            using var db = NewContext();
            var service = BuildService(db, out var memberId, out var deviceId);

            var device = await db.AttendanceDevices.FindAsync(deviceId);
            device!.IsActive = false;
            await db.SaveChangesAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.VerifyAsync(new VerifyFingerprintInput { DeviceId = deviceId, CapturedTemplate = Template("x") }));
        }

        [Fact]
        public async Task Dashboard_reflects_active_summary_and_recent()
        {
            using var db = NewContext();
            var service = BuildService(db, out var memberId, out var deviceId);

            await service.RegisterAsync(new FingerprintTemplateInput
            {
                MemberId = memberId, FingerPosition = 0, CapturedTemplate = Template("frank"), Vendor = "Mock"
            }, null);

            // One check-in -> member is inside.
            await service.VerifyAsync(new VerifyFingerprintInput { DeviceId = deviceId, CapturedTemplate = Template("frank") });

            var active = await service.GetActiveAttendanceAsync();
            Assert.Single(active);
            Assert.Equal(memberId, active[0].MemberId);
            Assert.Equal("Test Member", active[0].MemberName);

            var summaryAfterCheckIn = await service.GetSummaryAsync();
            Assert.Equal(1, summaryAfterCheckIn.CheckInsToday);
            Assert.Equal(0, summaryAfterCheckIn.CheckOutsToday);
            Assert.Equal(1, summaryAfterCheckIn.CurrentlyInside);

            // Check out.
            await service.VerifyAsync(new VerifyFingerprintInput { DeviceId = deviceId, CapturedTemplate = Template("frank") });

            Assert.Empty(await service.GetActiveAttendanceAsync());
            var summaryAfterCheckOut = await service.GetSummaryAsync();
            Assert.Equal(1, summaryAfterCheckOut.CheckOutsToday);
            Assert.Equal(0, summaryAfterCheckOut.CurrentlyInside);

            var recent = await service.GetRecentEventsAsync(10);
            Assert.NotEmpty(recent);
            Assert.Equal("check-out", recent[0].Action); // most recent event
        }

        [Fact]
        public async Task Delete_soft_deletes_template()
        {
            using var db = NewContext();
            var service = BuildService(db, out var memberId, out _);

            var output = await service.RegisterAsync(new FingerprintTemplateInput
            {
                MemberId = memberId, FingerPosition = 2, CapturedTemplate = Template("erin"), Vendor = "Mock"
            }, null);

            var deleted = await service.DeleteAsync(output.Id);

            Assert.True(deleted);
            var row = await db.FingerprintTemplates.SingleAsync();
            Assert.True(row.IsDeleted);
            // Soft-deleted templates are excluded from the member's active list.
            Assert.Empty(await service.GetByMemberAsync(memberId));
        }
    }
}
