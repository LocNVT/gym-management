using gym_management_server.DTOs.Common;
using gym_management_server.DTOs.InvoiceItems;
using gym_management_server.Entities.InvoiceItems;
using gym_management_server.Repositories.InvoiceItems;

namespace gym_management_server.Services.InvoiceItems
{
    public class InvoiceItemService
    {
        private readonly IInvoiceItemRepository _invoiceItemRepository;
        private readonly GymManagementServiceMapObjects _mapObjects;

        public InvoiceItemService(IInvoiceItemRepository invoiceItemRepository, GymManagementServiceMapObjects mapObjects)
        {
            _invoiceItemRepository = invoiceItemRepository;
            _mapObjects = mapObjects;
        }

        public async Task<PagedResult<InvoiceItemOutput>> GetAllAsync(int page = 1, int pageSize = 10)
        {
            var (items, totalCount) = await _invoiceItemRepository.GetPagedAsync(page, pageSize);
            var mapped = items.Select(i => _mapObjects.MapObjects<InvoiceItem, InvoiceItemOutput>(i)).ToList();
            return new PagedResult<InvoiceItemOutput> { Items = mapped, TotalCount = totalCount, Page = page, PageSize = pageSize };
        }

        public async Task<InvoiceItemOutput?> GetByIdAsync(Guid id)
        {
            var invoiceItem = await _invoiceItemRepository.GetByIdAsync(id);
            if (invoiceItem == null) return null;

            return _mapObjects.MapObjects<InvoiceItem, InvoiceItemOutput>(invoiceItem);
        }

        public async Task<InvoiceItemOutput> CreateAsync(InvoiceItemInput input)
        {
            var invoiceItem = new InvoiceItem();
            invoiceItem.Id = Guid.NewGuid();
            invoiceItem.InvoiceId = input.InvoiceId;
            invoiceItem.Description = input.Description;
            invoiceItem.Quantity = input.Quantity;
            invoiceItem.UnitPrice = input.UnitPrice;
            invoiceItem.ServicePackageId = input.ServicePackageId;

            await _invoiceItemRepository.AddAsync(invoiceItem);
            return _mapObjects.MapObjects<InvoiceItem, InvoiceItemOutput>(invoiceItem);
        }

        public async Task<InvoiceItemOutput?> UpdateAsync(Guid id, InvoiceItemInput input)
        {
            var invoiceItem = await _invoiceItemRepository.GetByIdAsync(id);
            if (invoiceItem == null) return null;

            invoiceItem.InvoiceId = input.InvoiceId;
            invoiceItem.Description = input.Description;
            invoiceItem.Quantity = input.Quantity;
            invoiceItem.UnitPrice = input.UnitPrice;
            invoiceItem.ServicePackageId = input.ServicePackageId;

            await _invoiceItemRepository.UpdateAsync(invoiceItem);
            return _mapObjects.MapObjects<InvoiceItem, InvoiceItemOutput>(invoiceItem);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var invoiceItem = await _invoiceItemRepository.GetByIdAsync(id);
            if (invoiceItem == null) return false;

            await _invoiceItemRepository.DeleteAsync(id);
            return true;
        }
    }
}
