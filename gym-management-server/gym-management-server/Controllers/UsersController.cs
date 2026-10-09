using gym_management_server.DTOs.Users;
using gym_management_server.Services.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace gym_management_server.Controllers
{
    /// <summary>
    /// Admin-only account management, scoped to the caller's own tenant - see
    /// docs/ImprovementPlan.md mục 1. Distinct from AuthController, which is for a user acting on
    /// their own account.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "1")]
    public class UsersController : ControllerBase
    {
        private readonly UserManagementService _service;

        public UsersController(UserManagementService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
            => Ok(await _service.GetListAsync(page, pageSize));

        [HttpPost]
        public async Task<IActionResult> Create(CreateUserInput input)
        {
            try
            {
                var created = await _service.CreateAsync(input);
                return Ok(created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}/active")]
        public async Task<IActionResult> SetActive(Guid id, SetUserActiveInput input)
        {
            var updated = await _service.SetActiveAsync(id, input.IsActive);
            return updated == null ? NotFound() : Ok(updated);
        }

        [HttpPut("{id}/role")]
        public async Task<IActionResult> SetRole(Guid id, SetUserRoleInput input)
        {
            var updated = await _service.SetRoleAsync(id, input.Role);
            return updated == null ? NotFound() : Ok(updated);
        }
    }
}
