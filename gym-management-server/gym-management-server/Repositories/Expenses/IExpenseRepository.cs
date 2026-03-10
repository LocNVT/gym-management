using gym_management_server.Entities.Expenses;

namespace gym_management_server.Repositories.Expenses
{
    public interface IExpenseRepository
    {
        Task<Expense?> GetByIdAsync(Guid id);
        Task<List<Expense>> GetAllAsync();
        Task<(List<Expense> Items, int TotalCount)> GetPagedAsync(int page, int pageSize);
        Task AddAsync(Expense expense);
        Task UpdateAsync(Expense expense);
        Task DeleteAsync(Guid id);
    }
}
