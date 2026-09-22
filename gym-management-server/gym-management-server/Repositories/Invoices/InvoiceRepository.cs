using gym_management_server.Data.EntityFramework;
using gym_management_server.DTOs.Export;
using gym_management_server.Entities.Invoices;
using Microsoft.EntityFrameworkCore;

namespace gym_management_server.Repositories.Invoices
{
    public class InvoiceRepository : IInvoiceRepository
    {
        private readonly GymManagementContext _db;

        public InvoiceRepository(GymManagementContext db)
        {
            _db = db;
        }

        public async Task AddAsync(Invoice invoice)
        {
            _db.Invoices.Add(invoice);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var invoice = await _db.Invoices.FirstOrDefaultAsync(x => x.Id == id);
            if (invoice != null)
            {
                _db.Remove(invoice);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<List<Invoice>> GetAllAsync()
        {
            return await _db.Invoices.ToListAsync();
        }

        public async Task<(List<Invoice> Items, int TotalCount)> GetPagedAsync(int page, int pageSize)
        {
            var query = _db.Invoices.AsQueryable();
            var totalCount = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (items, totalCount);
        }

        public async Task<Invoice?> GetByIdAsync(Guid id)
        {
            return await _db.Invoices.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task UpdateAsync(Invoice invoice)
        {
            _db.Invoices.Update(invoice);
            await _db.SaveChangesAsync();
        }

        public async Task<(List<InvoiceRow> Invoices, List<InvoiceItemRow> Items)> GetForExportAsync(DateTime? from, DateTime? to)
        {
            var query = _db.Invoices.AsQueryable();
            if (from.HasValue) query = query.Where(x => x.InvoiceDate >= from.Value.Date);
            // `to` is inclusive of the whole day the caller picked.
            if (to.HasValue) query = query.Where(x => x.InvoiceDate < to.Value.Date.AddDays(1));

            var invoices = await query
                .OrderByDescending(x => x.InvoiceDate)
                .Select(x => new InvoiceRow
                {
                    Id = x.Id,
                    InvoiceNumber = x.InvoiceNumber,
                    MemberName = x.Member != null ? x.Member.FullName : null,
                    MemberPhone = x.Member != null ? x.Member.PhoneNumber : null,
                    InvoiceDate = x.InvoiceDate,
                    TotalAmount = x.TotalAmount,
                    Status = x.Status,
                    PaymentMethod = x.PaymentMethod,
                    Notes = x.Notes,
                })
                .ToListAsync();

            var numbers = invoices.Select(i => i.InvoiceNumber).ToList();
            var items = await _db.InvoiceItems
                .Where(i => numbers.Contains(i.Invoice.InvoiceNumber))
                .Select(i => new InvoiceItemRow
                {
                    InvoiceNumber = i.Invoice.InvoiceNumber,
                    Description = i.Description,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    LineTotal = i.Quantity * i.UnitPrice,
                })
                .ToListAsync();

            return (invoices, items);
        }
    }
}
