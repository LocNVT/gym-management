using gym_management_server.DTOs.InvoiceItems;
using gym_management_server.Services.InvoiceItems;
using Microsoft.AspNetCore.Mvc;

namespace gym_management_server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvoiceItemController : ControllerBase
    {
        private readonly InvoiceItemService _service;

        public InvoiceItemController(InvoiceItemService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
            => Ok(await _service.GetAllAsync(page, pageSize));

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var invoiceItem = await _service.GetByIdAsync(id);
            return invoiceItem == null ? NotFound() : Ok(invoiceItem);
        }

        [HttpPost]
        public async Task<IActionResult> Create(InvoiceItemInput input)
        {
            var created = await _service.CreateAsync(input);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, InvoiceItemInput input)
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
