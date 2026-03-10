using gym_management_server.DTOs.Common;
using gym_management_server.DTOs.Invoices;
using gym_management_server.Entities.Invoices;
using gym_management_server.Repositories.Invoices;

namespace gym_management_server.Services.Invoices
{
    public class InvoiceService
    {
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly GymManagementServiceMapObjects _mapObjects;

        public InvoiceService(IInvoiceRepository invoiceRepository, GymManagementServiceMapObjects mapObjects)
        {
            _invoiceRepository = invoiceRepository;
            _mapObjects = mapObjects;
        }

        public async Task<PagedResult<InvoiceOutput>> GetAllAsync(int page = 1, int pageSize = 10)
        {
            var (items, totalCount) = await _invoiceRepository.GetPagedAsync(page, pageSize);
            var mapped = items.Select(i => _mapObjects.MapObjects<Invoice, InvoiceOutput>(i)).ToList();
            return new PagedResult<InvoiceOutput> { Items = mapped, TotalCount = totalCount, Page = page, PageSize = pageSize };
        }

        public async Task<InvoiceOutput?> GetByIdAsync(Guid id)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(id);
            if (invoice == null) return null;

            return _mapObjects.MapObjects<Invoice, InvoiceOutput>(invoice);
        }

        public async Task<InvoiceOutput> CreateAsync(InvoiceInput input)
        {
            var invoice = new Invoice();
            invoice.Id = Guid.NewGuid();
            invoice.InvoiceNumber = input.InvoiceNumber;
            invoice.MemberId = input.MemberId;
            invoice.InvoiceDate = input.InvoiceDate;
            invoice.TotalAmount = input.TotalAmount;
            invoice.Status = input.Status;
            invoice.PaymentMethod = input.PaymentMethod;
            invoice.Notes = input.Notes;

            await _invoiceRepository.AddAsync(invoice);
            return _mapObjects.MapObjects<Invoice, InvoiceOutput>(invoice);
        }

        public async Task<InvoiceOutput?> UpdateAsync(Guid id, InvoiceInput input)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(id);
            if (invoice == null) return null;

            invoice.InvoiceNumber = input.InvoiceNumber;
            invoice.MemberId = input.MemberId;
            invoice.InvoiceDate = input.InvoiceDate;
            invoice.TotalAmount = input.TotalAmount;
            invoice.Status = input.Status;
            invoice.PaymentMethod = input.PaymentMethod;
            invoice.Notes = input.Notes;

            await _invoiceRepository.UpdateAsync(invoice);
            return _mapObjects.MapObjects<Invoice, InvoiceOutput>(invoice);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(id);
            if (invoice == null) return false;

            await _invoiceRepository.DeleteAsync(id);
            return true;
        }
    }
}
