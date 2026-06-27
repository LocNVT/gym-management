using gym_management_server.Services.Fingerprints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace gym_management_server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "0,1")] // Staff or Admin may view attendance history
    public class AttendanceController : ControllerBase
    {
        private readonly FingerprintService _service;

        public AttendanceController(FingerprintService service)
        {
            _service = service;
        }

        /// <summary>Paged attendance history (check-in/out sessions) for a member, newest first.</summary>
        [HttpGet("member/{memberId}")]
        public async Task<IActionResult> GetByMember(Guid memberId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
            => Ok(await _service.GetAttendanceHistoryAsync(memberId, page, pageSize));

        /// <summary>Members currently checked in (open sessions), oldest first.</summary>
        [HttpGet("active")]
        public async Task<IActionResult> GetActive()
            => Ok(await _service.GetActiveAttendanceAsync());

        /// <summary>Headline counts for a day (defaults to today, UTC).</summary>
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary([FromQuery] DateTime? date = null)
            => Ok(await _service.GetSummaryAsync(date));

        /// <summary>Recent check-in/out events across all members for the activity feed.</summary>
        [HttpGet("recent")]
        public async Task<IActionResult> GetRecent([FromQuery] int count = 20)
            => Ok(await _service.GetRecentEventsAsync(count));
    }
}
