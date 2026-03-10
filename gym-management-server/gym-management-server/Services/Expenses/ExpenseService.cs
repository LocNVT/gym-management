using gym_management_server.DTOs.Common;
using gym_management_server.DTOs.Expenses;
using gym_management_server.Entities.Expenses;
using gym_management_server.Repositories.Expenses;

namespace gym_management_server.Services.Expenses
{
    public class ExpenseService
    {
        private readonly IExpenseRepository _expenseRepository;
        private readonly GymManagementServiceMapObjects _mapObjects;

        public ExpenseService(IExpenseRepository expenseRepository, GymManagementServiceMapObjects mapObjects)
        {
            _expenseRepository = expenseRepository;
            _mapObjects = mapObjects;
        }

        public async Task<PagedResult<ExpenseOutput>> GetAllAsync(int page = 1, int pageSize = 10)
        {
            var (items, totalCount) = await _expenseRepository.GetPagedAsync(page, pageSize);
            var mapped = items.Select(e => _mapObjects.MapObjects<Expense, ExpenseOutput>(e)).ToList();
            return new PagedResult<ExpenseOutput> { Items = mapped, TotalCount = totalCount, Page = page, PageSize = pageSize };
        }

        public async Task<ExpenseOutput?> GetByIdAsync(Guid id)
        {
            var expense = await _expenseRepository.GetByIdAsync(id);
            if (expense == null) return null;

            return _mapObjects.MapObjects<Expense, ExpenseOutput>(expense);
        }

        public async Task<ExpenseOutput> CreateAsync(ExpenseInput input)
        {
            var expense = new Expense();
            expense.Id = Guid.NewGuid();
            expense.ExpenseDate = input.ExpenseDate;
            expense.Category = input.Category;
            expense.Description = input.Description;
            expense.Amount = input.Amount;
            expense.PaymentMethod = input.PaymentMethod;
            expense.Notes = input.Notes;

            await _expenseRepository.AddAsync(expense);
            return _mapObjects.MapObjects<Expense, ExpenseOutput>(expense);
        }

        public async Task<ExpenseOutput?> UpdateAsync(Guid id, ExpenseInput input)
        {
            var expense = await _expenseRepository.GetByIdAsync(id);
            if (expense == null) return null;

            expense.ExpenseDate = input.ExpenseDate;
            expense.Category = input.Category;
            expense.Description = input.Description;
            expense.Amount = input.Amount;
            expense.PaymentMethod = input.PaymentMethod;
            expense.Notes = input.Notes;

            await _expenseRepository.UpdateAsync(expense);
            return _mapObjects.MapObjects<Expense, ExpenseOutput>(expense);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var expense = await _expenseRepository.GetByIdAsync(id);
            if (expense == null) return false;

            await _expenseRepository.DeleteAsync(id);
            return true;
        }
    }
}
