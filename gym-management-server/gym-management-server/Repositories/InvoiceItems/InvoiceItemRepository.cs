using gym_management_server.Data.EntityFramework;
using gym_management_server.Entities.InvoiceItems;
using Microsoft.EntityFrameworkCore;

namespace gym_management_server.Repositories.InvoiceItems
{
    public class InvoiceItemRepository : IInvoiceItemRepository
    {
        private readonly GymManagementContext _db;

        public InvoiceItemRepository(GymManagementContext db)
        {
            _db = db;
        }

        public async Task AddAsync(InvoiceItem invoiceItem)
        {
            _db.InvoiceItems.Add(invoiceItem);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var invoiceItem = await _db.InvoiceItems.FirstOrDefaultAsync(x => x.Id == id);
            if (invoiceItem != null)
            {
                _db.Remove(invoiceItem);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<List<InvoiceItem>> GetAllAsync()
        {
            return await _db.InvoiceItems.ToListAsync();
        }

        public async Task<(List<InvoiceItem> Items, int TotalCount)> GetPagedAsync(int page, int pageSize)
        {
            var query = _db.InvoiceItems.AsQueryable();
            var totalCount = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (items, totalCount);
        }

        public async Task<InvoiceItem?> GetByIdAsync(Guid id)
        {
            return await _db.InvoiceItems.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task UpdateAsync(InvoiceItem invoiceItem)
        {
            _db.InvoiceItems.Update(invoiceItem);
            await _db.SaveChangesAsync();
        }
    }
}
