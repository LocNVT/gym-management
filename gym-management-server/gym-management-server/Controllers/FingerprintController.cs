using System.Security.Claims;
using System.Security.Cryptography;
using gym_management_server.DTOs.Fingerprints;
using gym_management_server.Services.Fingerprints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace gym_management_server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "0,1")] // Staff or Admin may enrol/manage fingerprints
    public class FingerprintController : ControllerBase
    {
        private const string DeviceApiKeyHeader = "X-Device-Api-Key";

        private readonly FingerprintService _service;
        private readonly IConfiguration _configuration;

        public FingerprintController(FingerprintService service, IConfiguration configuration)
        {
            _service = service;
            _configuration = configuration;
        }

        /// <summary>List a member's registered fingerprints (template bytes are never returned).</summary>
        [HttpGet("member/{memberId}")]
        public async Task<IActionResult> GetByMember(Guid memberId)
            => Ok(await _service.GetByMemberAsync(memberId));

        /// <summary>Register (enrol) a new fingerprint template for a member.</summary>
        [HttpPost]
        public async Task<IActionResult> Register(FingerprintTemplateInput input)
        {
            var created = await _service.RegisterAsync(input, GetCurrentUserId());
            return CreatedAtAction(nameof(GetByMember), new { memberId = created.MemberId }, created);
        }

        /// <summary>Update an existing fingerprint template (re-enrol).</summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, FingerprintTemplateInput input)
        {
            var updated = await _service.UpdateAsync(id, input);
            return updated == null ? NotFound() : Ok(updated);
        }

        /// <summary>Soft-delete a registered fingerprint.</summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _service.DeleteAsync(id);
            return success ? NoContent() : NotFound();
        }

        /// <summary>
        /// Verify a scanned fingerprint and toggle attendance (check-in / check-out).
        /// Called by attendance devices; authenticated by a shared device API key rather than a
        /// user session. Send the key in the <c>X-Device-Api-Key</c> header. If
        /// <c>Fingerprint:DeviceApiKey</c> is not configured the check is skipped (dev/test).
        /// </summary>
        [HttpPost("verify")]
        [AllowAnonymous]
        public async Task<IActionResult> Verify(VerifyFingerprintInput input)
        {
            if (!IsDeviceAuthorized())
                return Unauthorized(new { message = "Invalid or missing device API key." });

            try
            {
                return Ok(await _service.VerifyAsync(input));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private bool IsDeviceAuthorized()
        {
            var expected = _configuration["Fingerprint:DeviceApiKey"];
            if (string.IsNullOrWhiteSpace(expected))
                return true; // not configured -> device auth disabled (development / tests)

            var provided = Request.Headers[DeviceApiKeyHeader].ToString();
            return CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(provided),
                System.Text.Encoding.UTF8.GetBytes(expected));
        }

        private Guid? GetCurrentUserId()
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }
}
