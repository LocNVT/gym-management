using System.Security.Claims;
using gym_management_server.Entities.CheckIns;
using gym_management_server.Infrastructure.Excel;
using gym_management_server.Services.CheckIns;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace gym_management_server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CheckInController : ControllerBase
    {
        private readonly CheckInService _service;

        public CheckInController(CheckInService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
            => Ok(await _service.GetListAsync(page, pageSize));

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var member = await _service.GetAsync(id);
            return member == null ? NotFound() : Ok(member);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CheckInCreateInput member)
        {
            try
            {
                var created = await _service.CreateAsync(member);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (DbUpdateException)
            {
                // Rejected by the unique "one open session per member" index (see
                // docs/ImprovementPlan.md mục 2) - most likely this member already has an open
                // check-in that was never checked out.
                return BadRequest(new { message = "Hội viên này đang có một phiên điểm danh chưa check-out. Vui lòng check-out phiên đó trước." });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, CheckInUpdateInput member)
        {
            var updated = await _service.UpdateAsync(id, member);
            return updated == null ? NotFound() : Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _service.DeleteAsync(id);
            return success ? NoContent() : NotFound();
        }

        /// <summary>Manual check-out, for staff to use when a member forgot to scan out.</summary>
        [HttpPost("{id}/checkout")]
        [Authorize]
        public async Task<IActionResult> CheckOut(Guid id)
        {
            try
            {
                var result = await _service.CheckOutAsync(id, GetCurrentUserId());
                return result == null ? NotFound() : Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private Guid? GetCurrentUserId()
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(raw, out var id) ? id : null;
        }

        [HttpGet("export")]
        [Authorize]
        public async Task<IActionResult> Export([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            try
            {
                return ExcelFileResult.File(await _service.ExportAsync(from, to), "lich-su-diem-danh");
            }
            catch (ExcelRowLimitExceededException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
