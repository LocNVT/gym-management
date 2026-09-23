using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace gym_management_server.Tests.Integration
{
    public class DashboardApiTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        public DashboardApiTests(CustomWebApplicationFactory factory) => _factory = factory;

        [Fact]
        public async Task Every_dashboard_endpoint_requires_authentication()
        {
            var client = _factory.CreateClient();
            foreach (var path in new[] { "kpi", "revenue-trend", "member-growth",
                                         "package-distribution", "peak-hours", "expiring-soon" })
            {
                var response = await client.GetAsync($"/api/Dashboard/{path}");
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        [Fact]
        public async Task Staff_are_refused_the_financial_endpoints()
        {
            var staff = _factory.CreateAuthenticatedClient(role: 0);

            Assert.Equal(HttpStatusCode.Forbidden, (await staff.GetAsync("/api/Dashboard/revenue-trend")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await staff.GetAsync("/api/Dashboard/package-distribution")).StatusCode);
        }

        [Fact]
        public async Task Staff_get_the_operational_KPIs_with_the_money_blanked_out()
        {
            // 403 on the whole endpoint would take the operational half of the page away too.
            var body = await _factory.CreateAuthenticatedClient(role: 0)
                .GetFromJsonAsync<JsonElement>("/api/Dashboard/kpi");

            Assert.Equal(JsonValueKind.Null, body.GetProperty("revenue").ValueKind);
            Assert.Equal(JsonValueKind.Null, body.GetProperty("profit").ValueKind);
            Assert.Equal(JsonValueKind.Null, body.GetProperty("unpaidTotal").ValueKind);

            Assert.Equal(JsonValueKind.Number, body.GetProperty("activeMembers").ValueKind);
            Assert.Equal(JsonValueKind.Number, body.GetProperty("currentlyInside").ValueKind);
            Assert.Equal(JsonValueKind.Number, body.GetProperty("expiringIn30Days").ValueKind);
        }

        [Fact]
        public async Task Admins_get_the_money()
        {
            var body = await _factory.CreateAuthenticatedClient(role: 1)
                .GetFromJsonAsync<JsonElement>("/api/Dashboard/kpi");

            Assert.Equal(JsonValueKind.Number, body.GetProperty("revenue").ValueKind);
            Assert.Equal(JsonValueKind.Number, body.GetProperty("profit").ValueKind);
        }

        [Fact]
        public async Task Kpi_defaults_to_the_current_local_month()
        {
            var body = await _factory.CreateAuthenticatedClient(role: 1)
                .GetFromJsonAsync<JsonElement>("/api/Dashboard/kpi");

            var now = gym_management_server.Infrastructure.Time.GymClock.LocalNow;
            Assert.Equal(now.Year, body.GetProperty("year").GetInt32());
            Assert.Equal(now.Month, body.GetProperty("month").GetInt32());
        }

        [Fact]
        public async Task Peak_hours_returns_24_buckets_even_with_no_data()
        {
            var body = await _factory.CreateAuthenticatedClient(role: 0)
                .GetFromJsonAsync<JsonElement>("/api/Dashboard/peak-hours");

            Assert.Equal(24, body.GetArrayLength());
        }

        [Fact]
        public async Task Months_zero_is_clamped_instead_of_crashing()
        {
            var admin = _factory.CreateAuthenticatedClient(role: 1);

            var response = await admin.GetAsync("/api/Dashboard/revenue-trend?months=0");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(body.GetArrayLength() >= 1);
        }

        [Fact]
        public async Task An_out_of_range_month_is_rejected_with_400_instead_of_crashing()
        {
            // GymClock.MonthRangeUtc does `new DateTime(year, month, 1)` unguarded; month=13
            // used to throw and surface as an unhandled 500.
            var admin = _factory.CreateAuthenticatedClient(role: 1);

            var response = await admin.GetAsync("/api/Dashboard/kpi?month=13");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("message").GetString()));
        }

        [Fact]
        public async Task An_out_of_range_year_is_rejected_with_400_instead_of_crashing()
        {
            var admin = _factory.CreateAuthenticatedClient(role: 1);

            var response = await admin.GetAsync("/api/Dashboard/kpi?year=0");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
