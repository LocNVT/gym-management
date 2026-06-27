using gym_management_server.DTOs.Trainers;
using gym_management_server.Services.Trainers;
using Microsoft.AspNetCore.Mvc;

namespace gym_management_server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TrainerController : ControllerBase
    {
        private readonly TrainerService _service;

        public TrainerController(TrainerService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
            => Ok(await _service.GetAllAsync(page, pageSize));

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var trainer = await _service.GetByIdAsync(id);
            return trainer == null ? NotFound() : Ok(trainer);
        }

        [HttpPost]
        public async Task<IActionResult> Create(TrainerInput input)
        {
            var created = await _service.CreateAsync(input);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, TrainerInput input)
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
    }
}
