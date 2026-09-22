using gym_management_server.DTOs.Devices;
using gym_management_server.Infrastructure.Excel;
using gym_management_server.Services.Devices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace gym_management_server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "1")] // Device management is Admin-only
    public class AttendanceDeviceController : ControllerBase
    {
        private readonly AttendanceDeviceService _service;

        public AttendanceDeviceController(AttendanceDeviceService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
            => Ok(await _service.GetAllAsync(page, pageSize));

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var device = await _service.GetByIdAsync(id);
            return device == null ? NotFound() : Ok(device);
        }

        [HttpPost]
        public async Task<IActionResult> Create(AttendanceDeviceInput input)
        {
            var created = await _service.CreateAsync(input);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, AttendanceDeviceInput input)
        {
            var updated = await _service.UpdateAsync(id, input);
            return updated == null ? NotFound() : Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _service.DeleteAsync(id);
            return success ? NoContent() : NotFound();
        }

        // No [Authorize] needed here: this endpoint inherits the controller's class-level
        // [Authorize(Roles = "1")] above, same as every other action in this controller.
        [HttpGet("export")]
        public async Task<IActionResult> Export()
        {
            try
            {
                return ExcelFileResult.File(await _service.ExportAsync(), "thiet-bi-diem-danh");
            }
            catch (ExcelRowLimitExceededException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
