using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace gym_management_server.Tests.Integration
{
    public class EnumsApiTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        public EnumsApiTests(CustomWebApplicationFactory factory) => _factory = factory;

        [Fact]
        public async Task Enums_endpoint_requires_authentication()
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/api/Enums");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Enums_endpoint_returns_every_enum_with_Vietnamese_labels()
        {
            var client = _factory.CreateAuthenticatedClient(role: 0);

            var payload = await client.GetFromJsonAsync<JsonElement>("/api/Enums");

            var invoiceStatus = payload.GetProperty("invoiceStatus");
            Assert.Equal(3, invoiceStatus.GetArrayLength());
            Assert.Equal(1, invoiceStatus[1].GetProperty("value").GetInt32());
            Assert.Equal("Đã thanh toán", invoiceStatus[1].GetProperty("label").GetString());

            foreach (var name in new[] { "memberStatus", "subscriptionStatus", "paymentMethod",
                                         "trainerStatus", "gender", "checkInMethod" })
                Assert.True(payload.TryGetProperty(name, out _), $"missing enum: {name}");
        }
    }
}
