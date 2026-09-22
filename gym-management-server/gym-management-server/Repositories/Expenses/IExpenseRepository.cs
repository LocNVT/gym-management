using gym_management_server.DTOs.Export;
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

        /// <summary>Expenses within [from, to] (inclusive of the whole `to` day), ordered by date, projected for Excel export.</summary>
        Task<List<ExpenseRow>> GetForExportAsync(DateTime? from, DateTime? to);
    }
}
