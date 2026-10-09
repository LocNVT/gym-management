using ClosedXML.Excel;
using gym_management_server.DTOs.Members;
using gym_management_server.Entities.Members;
using gym_management_server.Infrastructure.Excel;
using gym_management_server.Infrastructure.Images;
using gym_management_server.Services.Import;
using gym_management_server.Services.Members;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace gym_management_server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MemberController : ControllerBase
    {
        private const long MaxUploadBytes = 5 * 1024 * 1024;
        private const long MaxAvatarZipBytes = 50 * 1024 * 1024;

        private readonly MemberService _service;
        private readonly IWebHostEnvironment _env;
        private readonly MemberImportService _importService;

        public MemberController(MemberService service, IWebHostEnvironment env, MemberImportService importService)
        {
            _service = service;
            _env = env;
            _importService = importService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
            => Ok(await _service.GetAllMembersAsync(page, pageSize));

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var member = await _service.GetMemberByIdAsync(id);
            return member == null ? NotFound() : Ok(member);
        }

        [HttpPost]
        public async Task<IActionResult> Create(MemberInput member)
        {
            var created = await _service.CreateMemberAsync(member);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, MemberInput member)
        {
            var updated = await _service.UpdateMemberAsync(id, member);
            return updated == null ? NotFound() : Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _service.DeleteMemberAsync(id);
            return success ? NoContent() : NotFound();
        }

        [HttpPost("{id}/avatar")]
        public async Task<IActionResult> UploadAvatar(Guid id, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");
            if (file.Length > MaxUploadBytes)
                return BadRequest(new { message = "Ảnh vượt quá 5 MB." });

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!ImageFileValidator.AllowedExtensions.Contains(extension))
                return BadRequest("Invalid file type. Allowed: jpg, jpeg, png, gif, webp.");
            if (!await ImageFileValidator.LooksLikeImageAsync(file, extension))
                return BadRequest(new { message = "Nội dung file không khớp với định dạng ảnh đã khai báo." });

            var avatarUrl = await _service.UploadAvatarAsync(id, file, _env.WebRootPath);
            return avatarUrl == null ? NotFound() : Ok(new { avatarUrl });
        }

        /// <summary>
        /// Bulk-imports avatars from a .zip of "{PhoneNumber}.{ext}" files - see
        /// docs/ImprovementPlan.md mục 3. Admin-only: a bulk write across many members.
        /// </summary>
        [HttpPost("avatars/import")]
        [Authorize(Roles = "1")]
        public async Task<IActionResult> ImportAvatars(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Chưa chọn file." });
            if (file.Length > MaxAvatarZipBytes)
                return BadRequest(new { message = "File vượt quá giới hạn 50 MB." });
            if (!file.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Chỉ chấp nhận file .zip." });

            await using var stream = file.OpenReadStream();
            try
            {
                return Ok(await _service.ImportAvatarsAsync(stream, _env.WebRootPath));
            }
            catch (InvalidDataException)
            {
                return BadRequest(new { message = "Không thể đọc file này dưới dạng .zip hợp lệ." });
            }
        }

        /// <summary>Zips every member's current avatar for download. Admin-only.</summary>
        [HttpGet("avatars/export")]
        [Authorize(Roles = "1")]
        public async Task<IActionResult> ExportAvatars()
        {
            var zipBytes = await _service.ExportAvatarsAsync(_env.WebRootPath);
            return File(zipBytes, "application/zip", "anh-hoi-vien.zip");
        }

        [HttpGet("{id}/avatar")]
        public async Task<IActionResult> GetAvatar(Guid id)
        {
            var filePath = await _service.GetAvatarPathAsync(id, _env.WebRootPath);
            if (filePath == null) return NotFound();

            var contentType = Path.GetExtension(filePath).ToLowerInvariant() switch
            {
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => "image/jpeg"
            };

            return PhysicalFile(filePath, contentType);
        }

        [HttpGet("export")]
        [Authorize]
        public async Task<IActionResult> Export()
        {
            try
            {
                return ExcelFileResult.File(await _service.ExportAsync(), "thanh-vien");
            }
            catch (ExcelRowLimitExceededException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("import/template")]
        [Authorize]
        public IActionResult ImportTemplate() =>
            ExcelFileResult.File(ExcelTemplateWriter.Write(MemberSheet.Import), "mau-nhap-thanh-vien");

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

