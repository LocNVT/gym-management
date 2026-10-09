using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using gym_management_server.Data.EntityFramework;
using gym_management_server.DTOs.FaceRecognition;
using gym_management_server.Entities.CheckIns;
using gym_management_server.Entities.Devices;
using gym_management_server.Entities.Enums;
using gym_management_server.Entities.Members;
using gym_management_server.FaceRecognition;
using gym_management_server.FaceRecognition.Providers;
using gym_management_server.Fingerprints;
using gym_management_server.Repositories.CheckIns;
using gym_management_server.Repositories.Devices;
using gym_management_server.Repositories.FaceRecognition;
using gym_management_server.Repositories.Members;
using gym_management_server.Services;
using gym_management_server.Services.FaceRecognition;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace gym_management_server.Tests.Unit
{
    /// <summary>
    /// Mirrors FingerprintServiceTests.cs - see docs/ImprovementPlan.md mục 5. The full
    /// enrol -> verify -> check-in/out flow, exercised hardware-free via MockFaceRecognitionProvider.
    /// </summary>
    public class FaceRecognitionServiceTests
    {
        private static GymManagementContext NewContext()
        {
            var options = new DbContextOptionsBuilder<GymManagementContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new GymManagementContext(options);
        }

        private static FaceRecognitionService BuildService(GymManagementContext db, out Guid memberId, out Guid deviceId)
        {
            var member = new Member { Id = Guid.NewGuid(), FullName = "Test Member", PhoneNumber = "0900000000" };
            var device = new AttendanceDevice { Id = Guid.NewGuid(), Name = "Front Camera", Vendor = "Mock", IsActive = true };
            db.Members.Add(member);
            db.AttendanceDevices.Add(device);
            db.SaveChanges();

            memberId = member.Id;
            deviceId = device.Id;

            var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Fingerprint:EncryptionKey"] = key })
                .Build();

            var factory = new FaceRecognitionProviderFactory(new IFaceRecognitionProvider[] { new MockFaceRecognitionProvider() });

            return new FaceRecognitionService(
                new FaceTemplateRepository(db),
                new AttendanceDeviceRepository(db),
                new CheckInRepository(db),
                new MemberRepository(db),
                factory,
                new AesTemplateProtector(config),
                new GymManagementServiceMapObjects(),
                db.CurrentTenant);
        }

        private static string Template(string seed)
        {
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

            var output = await service.RegisterAsync(new FaceTemplateInput
            {
                MemberId = memberId,
                CapturedTemplate = Template("alice"),
                Vendor = "Mock",
                Quality = 88
            }, createdBy: null);

            Assert.Equal(memberId, output.MemberId);
            Assert.Equal((byte)88, output.Quality);

            var stored = await db.FaceTemplates.SingleAsync();
            Assert.NotEqual(Convert.FromBase64String(Template("alice")), stored.Template);
        }

        [Fact]
        public async Task Verify_creates_checkin_with_Face_method_when_no_active_session()
        {
            using var db = NewContext();
            var service = BuildService(db, out var memberId, out var deviceId);

            await service.RegisterAsync(new FaceTemplateInput
            {
                MemberId = memberId, CapturedTemplate = Template("bob"), Vendor = "Mock"
            }, null);

            var result = await service.VerifyAsync(new VerifyFaceInput
            {
                DeviceId = deviceId, CapturedTemplate = Template("bob")
            });

            Assert.True(result.Matched);
            Assert.Equal("check-in", result.Action);
            Assert.Equal(memberId, result.MemberId);

            var session = await db.CheckIns.SingleAsync();
            Assert.Null(session.CheckOutTime);
            Assert.Equal(CheckInMethod.Face, session.Method);
            Assert.Equal(deviceId, session.DeviceId);
        }

        [Fact]
        public async Task Verify_checks_out_when_active_session_exists()
        {
            using var db = NewContext();
            var service = BuildService(db, out var memberId, out var deviceId);

            await service.RegisterAsync(new FaceTemplateInput
            {
                MemberId = memberId, CapturedTemplate = Template("carol"), Vendor = "Mock"
            }, null);

            var first = await service.VerifyAsync(new VerifyFaceInput { DeviceId = deviceId, CapturedTemplate = Template("carol") });
            var second = await service.VerifyAsync(new VerifyFaceInput { DeviceId = deviceId, CapturedTemplate = Template("carol") });

            Assert.Equal("check-in", first.Action);
            Assert.Equal("check-out", second.Action);
            Assert.Equal(first.CheckInId, second.CheckInId);

            var session = await db.CheckIns.SingleAsync();
            Assert.NotNull(session.CheckOutTime);
            Assert.Equal(CheckOutMethod.Scan, session.CheckOutMethod);
        }

        [Fact]
        public async Task Verify_returns_no_match_for_an_unknown_face()
        {
            using var db = NewContext();
            var service = BuildService(db, out var memberId, out var deviceId);

            await service.RegisterAsync(new FaceTemplateInput
            {
                MemberId = memberId, CapturedTemplate = Template("known"), Vendor = "Mock"
            }, null);

            var result = await service.VerifyAsync(new VerifyFaceInput
            {
                DeviceId = deviceId, CapturedTemplate = Template("totally-different-face-xyz")
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
                service.VerifyAsync(new VerifyFaceInput { DeviceId = deviceId, CapturedTemplate = Template("x") }));
        }

        [Fact]
        public async Task Delete_soft_deletes_template()
        {
            using var db = NewContext();
            var service = BuildService(db, out var memberId, out _);

            var output = await service.RegisterAsync(new FaceTemplateInput
            {
                MemberId = memberId, CapturedTemplate = Template("erin"), Vendor = "Mock"
            }, null);

            var deleted = await service.DeleteAsync(output.Id);

            Assert.True(deleted);
            var row = await db.FaceTemplates.SingleAsync();
            Assert.True(row.IsDeleted);
            Assert.Empty(await service.GetByMemberAsync(memberId));
        }
    }
}
