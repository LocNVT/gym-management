using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using gym_management_server.DTOs.FaceRecognition;
using gym_management_server.DTOs.Fingerprints;
using gym_management_server.Entities.Devices;
using gym_management_server.Entities.Members;
using gym_management_server.FaceRecognition;
using gym_management_server.FaceRecognition.Providers;
using gym_management_server.Fingerprints;
using gym_management_server.Fingerprints.Providers;
using gym_management_server.Repositories.CheckIns;
using gym_management_server.Repositories.Devices;
using gym_management_server.Repositories.FaceRecognition;
using gym_management_server.Repositories.Fingerprints;
using gym_management_server.Repositories.Members;
using gym_management_server.Services;
using gym_management_server.Services.FaceRecognition;
using gym_management_server.Services.Fingerprints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace gym_management_server.Tests.Integration
{
    /// <summary>
    /// Covers the actual guarantee docs/ImprovementPlan.md mục 1 exists for: one tenant's data,
    /// including biometric templates, must never be visible to - or matchable from - another.
    /// </summary>
    public class MultiTenantIsolationTests
    {
        private static string Template(string seed)
        {
            var bytes = new byte[32];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = (byte)((seed.GetHashCode() >> (i % 24)) ^ (seed[i % seed.Length] + i));
            return Convert.ToBase64String(bytes);
        }

        [Fact]
        public async Task A_member_query_never_sees_another_tenants_rows()
        {
            using var fixture = new SqliteDbFixture();
            var db = fixture.NewContext();

            var tenantA = Guid.NewGuid();
            var tenantB = Guid.NewGuid();
            db.Members.Add(new Member { Id = Guid.NewGuid(), TenantId = tenantA, FullName = "Member A", PhoneNumber = "0900000001" });
            db.Members.Add(new Member { Id = Guid.NewGuid(), TenantId = tenantB, FullName = "Member B", PhoneNumber = "0900000001" }); // same phone, different tenant - must not collide
            await db.SaveChangesAsync();

            db.CurrentTenant.SetTenant(tenantA);
            var seenByA = await db.Members.ToListAsync();
            Assert.Single(seenByA);
            Assert.Equal("Member A", seenByA[0].FullName);

            db.CurrentTenant.SetTenant(tenantB);
            var seenByB = await db.Members.ToListAsync();
            Assert.Single(seenByB);
            Assert.Equal("Member B", seenByB[0].FullName);
        }

        [Fact]
        public async Task A_fingerprint_scan_on_one_tenants_device_can_never_match_another_tenants_member()
        {
            using var fixture = new SqliteDbFixture();
            var db = fixture.NewContext();

            var tenantA = Guid.NewGuid();
            var tenantB = Guid.NewGuid();

            var memberA = new Member { Id = Guid.NewGuid(), TenantId = tenantA, FullName = "Tenant A Member", PhoneNumber = "0911000001" };
            var deviceA = new AttendanceDevice { Id = Guid.NewGuid(), TenantId = tenantA, Name = "Door A", Vendor = "Mock", IsActive = true };
            var memberB = new Member { Id = Guid.NewGuid(), TenantId = tenantB, FullName = "Tenant B Member", PhoneNumber = "0922000002" };
            var deviceB = new AttendanceDevice { Id = Guid.NewGuid(), TenantId = tenantB, Name = "Door B", Vendor = "Mock", IsActive = true };
            db.Members.AddRange(memberA, memberB);
            db.AttendanceDevices.AddRange(deviceA, deviceB);
            await db.SaveChangesAsync();

            var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Fingerprint:EncryptionKey"] = key })
                .Build();
            var protector = new AesTemplateProtector(config);
            var factory = new FingerprintProviderFactory(new IFingerprintProvider[] { new MockFingerprintProvider() });
            var service = new FingerprintService(
                new FingerprintTemplateRepository(db),
                new AttendanceDeviceRepository(db),
                new CheckInRepository(db),
                new MemberRepository(db),
                factory,
                protector,
                new GymManagementServiceMapObjects(),
                db.CurrentTenant);

            // Enrol member B's fingerprint while acting as tenant B (as the authenticated
            // enrolment endpoint would - RegisterAsync doesn't resolve a tenant itself).
            db.CurrentTenant.SetTenant(tenantB);
            await service.RegisterAsync(new FingerprintTemplateInput
            {
                MemberId = memberB.Id,
                FingerPosition = 0,
                CapturedTemplate = Template("shared-finger"),
                Vendor = "Mock"
            }, createdBy: null);

            // Tenant A's device scans the SAME physical fingerprint content. Even though the
            // template bytes match, member B's template must be invisible to a tenant-A device.
            var result = await service.VerifyAsync(new VerifyFingerprintInput
            {
                DeviceId = deviceA.Id,
                CapturedTemplate = Template("shared-finger")
            });

            Assert.False(result.Matched);
            Assert.Null(result.MemberId);
        }

        [Fact]
        public async Task A_fingerprint_scan_still_matches_the_right_member_within_the_same_tenant()
        {
            using var fixture = new SqliteDbFixture();
            var db = fixture.NewContext();

            var tenant = Guid.NewGuid();
            var member = new Member { Id = Guid.NewGuid(), TenantId = tenant, FullName = "Same Tenant Member", PhoneNumber = "0933000003" };
            var device = new AttendanceDevice { Id = Guid.NewGuid(), TenantId = tenant, Name = "Door", Vendor = "Mock", IsActive = true };
            db.Members.Add(member);
            db.AttendanceDevices.Add(device);
            await db.SaveChangesAsync();

            var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Fingerprint:EncryptionKey"] = key })
                .Build();
            var service = new FingerprintService(
                new FingerprintTemplateRepository(db),
                new AttendanceDeviceRepository(db),
                new CheckInRepository(db),
                new MemberRepository(db),
                new FingerprintProviderFactory(new IFingerprintProvider[] { new MockFingerprintProvider() }),
                new AesTemplateProtector(config),
                new GymManagementServiceMapObjects(),
                db.CurrentTenant);

            db.CurrentTenant.SetTenant(tenant);
            await service.RegisterAsync(new FingerprintTemplateInput
            {
                MemberId = member.Id,
                FingerPosition = 0,
                CapturedTemplate = Template("same-tenant-finger"),
                Vendor = "Mock"
            }, createdBy: null);

            var result = await service.VerifyAsync(new VerifyFingerprintInput
            {
                DeviceId = device.Id,
                CapturedTemplate = Template("same-tenant-finger")
            });

            Assert.True(result.Matched);
            Assert.Equal(member.Id, result.MemberId);
        }

        [Fact]
        public async Task A_face_scan_on_one_tenants_device_can_never_match_another_tenants_member()
        {
            using var fixture = new SqliteDbFixture();
            var db = fixture.NewContext();

            var tenantA = Guid.NewGuid();
            var tenantB = Guid.NewGuid();

            var memberA = new Member { Id = Guid.NewGuid(), TenantId = tenantA, FullName = "Tenant A Member", PhoneNumber = "0911000011" };
            var deviceA = new AttendanceDevice { Id = Guid.NewGuid(), TenantId = tenantA, Name = "Camera A", Vendor = "Mock", IsActive = true };
            var memberB = new Member { Id = Guid.NewGuid(), TenantId = tenantB, FullName = "Tenant B Member", PhoneNumber = "0922000022" };
            var deviceB = new AttendanceDevice { Id = Guid.NewGuid(), TenantId = tenantB, Name = "Camera B", Vendor = "Mock", IsActive = true };
            db.Members.AddRange(memberA, memberB);
            db.AttendanceDevices.AddRange(deviceA, deviceB);
            await db.SaveChangesAsync();

            var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Fingerprint:EncryptionKey"] = key })
                .Build();
            var protector = new AesTemplateProtector(config);
            var factory = new FaceRecognitionProviderFactory(new IFaceRecognitionProvider[] { new MockFaceRecognitionProvider() });
            var service = new FaceRecognitionService(
                new FaceTemplateRepository(db),
                new AttendanceDeviceRepository(db),
                new CheckInRepository(db),
                new MemberRepository(db),
                factory,
                protector,
                new GymManagementServiceMapObjects(),
                db.CurrentTenant);

            // Enrol member B's face while acting as tenant B.
            db.CurrentTenant.SetTenant(tenantB);
            await service.RegisterAsync(new FaceTemplateInput
            {
                MemberId = memberB.Id,
                CapturedTemplate = Template("shared-face"),
                Vendor = "Mock"
            }, createdBy: null);

            // Tenant A's camera scans the SAME face content. Even though the bytes match, member
            // B's template must be invisible to a tenant-A device.
            var result = await service.VerifyAsync(new VerifyFaceInput
            {
                DeviceId = deviceA.Id,
                CapturedTemplate = Template("shared-face")
            });

            Assert.False(result.Matched);
            Assert.Null(result.MemberId);
        }
    }
}
