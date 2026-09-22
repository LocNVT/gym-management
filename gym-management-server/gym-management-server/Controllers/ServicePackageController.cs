using ClosedXML.Excel;
using gym_management_server.DTOs.ServicePackages;
using gym_management_server.Infrastructure.Excel;
using gym_management_server.Services.Import;
using gym_management_server.Services.ServicePackages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace gym_management_server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServicePackageController : ControllerBase
    {
        private const long MaxUploadBytes = 5 * 1024 * 1024;

        private readonly ServicePackageService _service;
        private readonly ServicePackageImportService _importService;

        public ServicePackageController(ServicePackageService service, ServicePackageImportService importService)
        {
            _service = service;
            _importService = importService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
            => Ok(await _service.GetAllAsync(page, pageSize));

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var servicePackage = await _service.GetByIdAsync(id);
            return servicePackage == null ? NotFound() : Ok(servicePackage);
        }

        [HttpPost]
        public async Task<IActionResult> Create(ServicePackageInput input)
        {
            var created = await _service.CreateAsync(input);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, ServicePackageInput input)
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

        [HttpGet("export")]
        [Authorize]
        public async Task<IActionResult> Export()
        {
            try
            {
                return ExcelFileResult.File(await _service.ExportAsync(), "goi-dich-vu");
            }
            catch (ExcelRowLimitExceededException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("import/template")]
        [Authorize]
        public IActionResult ImportTemplate() =>
            ExcelFileResult.File(ExcelTemplateWriter.Write(ServicePackageSheet.Import), "mau-nhap-goi-dich-vu");

        /// <summary>
        /// dryRun=true validates and writes nothing. dryRun=false writes, but only if the file is
        /// completely clean. There is no server-side state between the two calls: the client sends
        /// the same file twice.
        /// </summary>
        [HttpPost("import")]
        [Authorize]
        public async Task<IActionResult> Import(IFormFile? file, [FromQuery] bool dryRun = true)
        {
            if (file is null || file.Length == 0)
                return BadRequest(new { message = "Chưa chọn file." });
            if (file.Length > MaxUploadBytes)
                return BadRequest(new { message = "File vượt quá 5 MB." });
            if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Chỉ chấp nhận file .xlsx." });

            // ClosedXML's XLWorkbook constructor throws NullReferenceException, not a typed
            // exception, for a file that is a well-formed ZIP but not a real OOXML workbook --
            // the shape a truncated or corrupted download takes. Genuine non-zip garbage throws
            // a typed FileFormatException (a FormatException) instead, already handled by the
            // catch below. This probe is scoped to just the parse, not the whole import, so a
            // real server fault later in the pipeline (a database failure, say) still propagates
            // as a 500 rather than being reported to the user as "bad file".
            try
            {
                await using var probeStream = file.OpenReadStream();
                using var probe = new XLWorkbook(probeStream);
            }
            catch (Exception)
            {
                return BadRequest(new
                {
                    message = "Không thể đọc file này dưới dạng workbook Excel. Vui lòng xuất lại hoặc tải lại file."
                });
            }

            await using var stream = file.OpenReadStream();
            try
            {
                return Ok(await _importService.ImportAsync(stream, dryRun));
            }
            catch (Exception ex) when (ex is InvalidOperationException or FormatException)
            {
                return BadRequest(new { message = $"Không đọc được file: {ex.Message}" });
            }
        }
    }
}
