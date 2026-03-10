using gym_management_server.Data.EntityFramework;
using gym_management_server.Entities.Expenses;
using Microsoft.EntityFrameworkCore;

namespace gym_management_server.Repositories.Expenses
{
    public class ExpenseRepository : IExpenseRepository
    {
        private readonly GymManagementContext _db;

        public ExpenseRepository(GymManagementContext db)
        {
            _db = db;
        }

        public async Task AddAsync(Expense expense)
        {
            _db.Expenses.Add(expense);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var expense = await _db.Expenses.FirstOrDefaultAsync(x => x.Id == id);
            if (expense != null)
            {
                _db.Remove(expense);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<List<Expense>> GetAllAsync()
        {
            return await _db.Expenses.ToListAsync();
        }

        public async Task<(List<Expense> Items, int TotalCount)> GetPagedAsync(int page, int pageSize)
        {
            var query = _db.Expenses.AsQueryable();
            var totalCount = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (items, totalCount);
        }

        public async Task<Expense?> GetByIdAsync(Guid id)
        {
            return await _db.Expenses.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task UpdateAsync(Expense expense)
        {
            _db.Expenses.Update(expense);
            await _db.SaveChangesAsync();
        }
    }
}
