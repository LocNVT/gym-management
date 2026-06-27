using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using gym_management_server.Data.EntityFramework;
using gym_management_server.DTOs.Fingerprints;
using gym_management_server.Entities.Devices;
using gym_management_server.Entities.Members;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace gym_management_server.Tests.Integration
{
    public class FingerprintApiTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public FingerprintApiTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private (Guid memberId, Guid deviceId) Seed()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GymManagementContext>();

            var member = new Member { Id = Guid.NewGuid(), FullName = "API Member", PhoneNumber = "0911" + Guid.NewGuid().ToString("N")[..6] };
            var device = new AttendanceDevice { Id = Guid.NewGuid(), Name = "API Door", Vendor = "Mock", IsActive = true };
            db.Members.Add(member);
            db.AttendanceDevices.Add(device);
            db.SaveChanges();
            return (member.Id, device.Id);
        }

        private static string Template(string seed)
        {
            var bytes = new byte[32];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = (byte)((seed[i % seed.Length] + i) & 0xff);
            return Convert.ToBase64String(bytes);
        }

        [Fact]
        public async Task DeviceManagement_requires_authentication()
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/api/AttendanceDevice");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Fingerprint_enrolment_requires_authentication()
        {
            var (memberId, _) = Seed();
            var client = _factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/Fingerprint", new FingerprintTemplateInput
            {
                MemberId = memberId, FingerPosition = 0, CapturedTemplate = Template("x"), Vendor = "Mock"
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Verify_endpoint_is_anonymous_and_returns_no_match_when_empty()
        {
            var (_, deviceId) = Seed();
            var client = _factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/Fingerprint/verify", new VerifyFingerprintInput
            {
                DeviceId = deviceId, CapturedTemplate = Template("nobody-enrolled")
            });

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<VerifyFingerprintResult>();
            Assert.NotNull(result);
            Assert.False(result!.Matched);
            Assert.Equal("none", result.Action);
        }

        [Fact]
        public async Task Verify_with_unknown_device_returns_bad_request()
        {
            var client = _factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/Fingerprint/verify", new VerifyFingerprintInput
            {
                DeviceId = Guid.NewGuid(), CapturedTemplate = Template("x")
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
