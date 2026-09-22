using gym_management_server.Entities.MemberDataServices;
using gym_management_server.Infrastructure.Excel;
using gym_management_server.Services.MemberDataServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace gym_management_server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MemberDataServiceController : ControllerBase
    {
        private readonly MemberDataServiceService _service;

        public MemberDataServiceController(MemberDataServiceService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10) => Ok(await _service.GetAllAsync(page, pageSize));

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var output = await _service.GetAsync(id);
            return output == null ? NotFound() : Ok(output);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _service.DeleteMemberAsync(id);
            return success ? NoContent() : NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> Create(MemberDataServiceInput input)
        {
            var memberDataService = await _service.CreateAsync(input);
            return CreatedAtAction(nameof(GetById), new { id = memberDataService.Id }, memberDataService);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, MemberDataServiceInput input)
        {
            var memberDataService = await _service.UpdateAsync(id, input);
            return memberDataService == null ? NotFound() : Ok(memberDataService);
        }

        [HttpGet("export")]
        [Authorize]
        public async Task<IActionResult> Export([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            try
            {
                return ExcelFileResult.File(await _service.ExportAsync(from, to), "dang-ky-goi");
            }
            catch (ExcelRowLimitExceededException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
