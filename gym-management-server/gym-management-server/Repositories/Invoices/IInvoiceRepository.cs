using gym_management_server.Entities.Invoices;

namespace gym_management_server.Repositories.Invoices
{
    public interface IInvoiceRepository
    {
        Task<Invoice?> GetByIdAsync(Guid id);
        Task<List<Invoice>> GetAllAsync();
        Task<(List<Invoice> Items, int TotalCount)> GetPagedAsync(int page, int pageSize);
        Task AddAsync(Invoice invoice);
        Task UpdateAsync(Invoice invoice);
        Task DeleteAsync(Guid id);
    }
}
