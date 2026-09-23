using gym_management_server.Services.Reporting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace gym_management_server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly DashboardService _service;
        public DashboardController(DashboardService service) => _service = service;

        private bool IsAdmin => User.IsInRole("1");

        /// <summary>
        /// One endpoint per widget rather than one big payload: the KPI row must paint
        /// immediately and should not wait for the check-in aggregation.
        /// </summary>
        [HttpGet("kpi")]
        public async Task<IActionResult> Kpi([FromQuery] int? year = null, [FromQuery] int? month = null)
            => Ok(await _service.GetKpiAsync(year, month, includeFinancials: IsAdmin));

        [HttpGet("revenue-trend")]
        [Authorize(Roles = "1")]
        public async Task<IActionResult> RevenueTrend([FromQuery] int months = 12)
            => Ok(await _service.GetRevenueTrendAsync(months));

        [HttpGet("member-growth")]
        public async Task<IActionResult> MemberGrowth([FromQuery] int months = 12)
            => Ok(await _service.GetMemberGrowthAsync(months));

        [HttpGet("package-distribution")]
        [Authorize(Roles = "1")]
        public async Task<IActionResult> PackageDistribution()
            => Ok(await _service.GetPackageDistributionAsync());

        [HttpGet("peak-hours")]
        public async Task<IActionResult> PeakHours([FromQuery] int days = 30)
            => Ok(await _service.GetPeakHoursAsync(days));

        [HttpGet("expiring-soon")]
        public async Task<IActionResult> ExpiringSoon([FromQuery] int days = 30)
            => Ok(await _service.GetExpiringSoonAsync(days));
    }
}
