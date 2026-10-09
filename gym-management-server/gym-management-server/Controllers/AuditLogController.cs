using gym_management_server.Services.Auditing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace gym_management_server.Controllers
{
    /// <summary>Read-only audit trail for Admins - see docs/ImprovementPlan.md mục 6.</summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "1")]
    public class AuditLogController : ControllerBase
    {
        private readonly AuditLogService _service;

        public AuditLogController(AuditLogService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> Query(
            [FromQuery] string? entityName = null,
            [FromQuery] Guid? actorUserId = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
            => Ok(await _service.QueryAsync(entityName, actorUserId, from, to, page, pageSize));
    }
}
