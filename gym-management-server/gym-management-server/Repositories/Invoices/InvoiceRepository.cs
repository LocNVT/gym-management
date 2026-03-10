using gym_management_server.Data.EntityFramework;
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
    }
}
