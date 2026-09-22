using gym_management_server.DTOs.Export;
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

        /// <summary>
        /// Invoices dated within [from, to] (inclusive of the whole `to` day), newest first, with the
        /// member's name/phone resolved via the (nullable) Member navigation, plus their line items.
        /// </summary>
        Task<(List<InvoiceRow> Invoices, List<InvoiceItemRow> Items)> GetForExportAsync(DateTime? from, DateTime? to);
    }
}
