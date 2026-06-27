using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using gym_management_server.Data.EntityFramework;
using gym_management_server.DTOs.Fingerprints;
using gym_management_server.Entities.Devices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace gym_management_server.Tests.Integration
{
    /// <summary>Factory variant that DOES configure a device API key, enabling the verify-endpoint check.</summary>
    public class ApiKeyWebApplicationFactory : CustomWebApplicationFactory
    {
        public const string DeviceApiKey = "test-device-key-123";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Fingerprint:DeviceApiKey"] = DeviceApiKey
                });
            });
        }
    }

    public class DeviceApiKeyTests : IClassFixture<ApiKeyWebApplicationFactory>
    {
        private readonly ApiKeyWebApplicationFactory _factory;

        public DeviceApiKeyTests(ApiKeyWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private Guid SeedDevice()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GymManagementContext>();
            var device = new AttendanceDevice { Id = Guid.NewGuid(), Name = "Keyed Door", Vendor = "Mock", IsActive = true };
            db.AttendanceDevices.Add(device);
            db.SaveChanges();
            return device.Id;
        }

        private static VerifyFingerprintInput Payload(Guid deviceId) => new()
        {
            DeviceId = deviceId,
            CapturedTemplate = Convert.ToBase64String(new byte[32])
        };

        [Fact]
        public async Task Verify_without_api_key_is_unauthorized()
        {
            var deviceId = SeedDevice();
            var client = _factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/Fingerprint/verify", Payload(deviceId));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Verify_with_wrong_api_key_is_unauthorized()
        {
            var deviceId = SeedDevice();
            var client = _factory.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Post, "/api/Fingerprint/verify")
            {
                Content = JsonContent.Create(Payload(deviceId))
            };
            request.Headers.Add("X-Device-Api-Key", "wrong-key");

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Verify_with_correct_api_key_succeeds()
        {
            var deviceId = SeedDevice();
            var client = _factory.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Post, "/api/Fingerprint/verify")
            {
                Content = JsonContent.Create(Payload(deviceId))
            };
            request.Headers.Add("X-Device-Api-Key", ApiKeyWebApplicationFactory.DeviceApiKey);

            var response = await client.SendAsync(request);

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<VerifyFingerprintResult>();
            Assert.NotNull(result);
            Assert.False(result!.Matched); // no templates enrolled, but auth passed
        }
    }
}
