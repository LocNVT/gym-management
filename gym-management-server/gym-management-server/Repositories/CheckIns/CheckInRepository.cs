using gym_management_server.Data.EntityFramework;
using gym_management_server.Entities.CheckIns;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace gym_management_server.Repositories.CheckIns
{
    public class CheckInRepository : ICheckInRepository
    {
        private readonly GymManagementContext _db;

        public CheckInRepository(GymManagementContext db)
        {
            _db = db;
        }

        public async Task AddAsync(CheckIn checkIn)
        {
            _db.CheckIns.Add(checkIn);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var checkIn = await _db.CheckIns.FirstOrDefaultAsync(x => x.Id == id);
            if (checkIn != null)
            {
                _db.Remove(checkIn);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<List<CheckIn>> GetAllAsync(Expression<Func<CheckIn, bool>>? predicate = null)
        {
            IQueryable<CheckIn> query = _db.CheckIns;

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            return await query.ToListAsync();
        }

        public async Task<CheckIn?> GetByIdAsync(Guid id)
        {
            return await _db.CheckIns.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<(List<CheckIn> Items, int TotalCount)> GetPagedAsync(int page, int pageSize)
        {
            var query = _db.CheckIns.AsQueryable();
            var totalCount = await query.CountAsync();
            var items = await query.OrderByDescending(x => x.CheckInTime).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (items, totalCount);
        }

        public async Task UpdateAsync(CheckIn checkIn)
        {
            _db.Update(checkIn);
            await _db.SaveChangesAsync();
        }
    }
}
