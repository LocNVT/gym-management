using gym_management_server.Entities.InvoiceItems;

namespace gym_management_server.Repositories.InvoiceItems
{
    public interface IInvoiceItemRepository
    {
        Task<InvoiceItem?> GetByIdAsync(Guid id);
        Task<List<InvoiceItem>> GetAllAsync();
        Task<(List<InvoiceItem> Items, int TotalCount)> GetPagedAsync(int page, int pageSize);
        Task AddAsync(InvoiceItem invoiceItem);
        Task UpdateAsync(InvoiceItem invoiceItem);
        Task DeleteAsync(Guid id);
    }
}
