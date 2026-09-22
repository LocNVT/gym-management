using gym_management_server.DTOs.Export;
using gym_management_server.Entities.Trainers;

namespace gym_management_server.Repositories.Trainers
{
    public interface ITrainerRepository
    {
        Task<Trainer?> GetByIdAsync(Guid id);
        Task<List<Trainer>> GetAllAsync();
        Task<(List<Trainer> Items, int TotalCount)> GetPagedAsync(int page, int pageSize);
        Task AddAsync(Trainer trainer);
        Task UpdateAsync(Trainer trainer);
        Task DeleteAsync(Guid id);

        /// <summary>All trainers, ordered by name, projected for Excel export.</summary>
        Task<List<TrainerRow>> GetForExportAsync();
    }
}
