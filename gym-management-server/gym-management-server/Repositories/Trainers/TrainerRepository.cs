using gym_management_server.Data.EntityFramework;
using gym_management_server.Entities.Trainers;
using Microsoft.EntityFrameworkCore;

namespace gym_management_server.Repositories.Trainers
{
    public class TrainerRepository : ITrainerRepository
    {
        private readonly GymManagementContext _db;

        public TrainerRepository(GymManagementContext db)
        {
            _db = db;
        }

        public async Task AddAsync(Trainer trainer)
        {
            _db.Trainers.Add(trainer);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var trainer = await _db.Trainers.FirstOrDefaultAsync(x => x.Id == id);
            if (trainer != null)
            {
                _db.Remove(trainer);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<List<Trainer>> GetAllAsync()
        {
            return await _db.Trainers.ToListAsync();
        }

        public async Task<(List<Trainer> Items, int TotalCount)> GetPagedAsync(int page, int pageSize)
        {
            var query = _db.Trainers.AsQueryable();
            var totalCount = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (items, totalCount);
        }

        public async Task<Trainer?> GetByIdAsync(Guid id)
        {
            return await _db.Trainers.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task UpdateAsync(Trainer trainer)
        {
            _db.Trainers.Update(trainer);
            await _db.SaveChangesAsync();
        }
    }
}
