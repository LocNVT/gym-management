using gym_management_server.Data.EntityFramework;
using gym_management_server.DTOs.Export;
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

        public async Task<CheckIn?> GetActiveByMemberAsync(Guid memberId)
        {
            return await _db.CheckIns
                .Where(x => x.MemberId == memberId && x.CheckOutTime == null)
                .OrderByDescending(x => x.CheckInTime)
                .FirstOrDefaultAsync();
        }

        public async Task<(List<CheckIn> Items, int TotalCount)> GetByMemberPagedAsync(Guid memberId, int page, int pageSize)
        {
            var query = _db.CheckIns.Where(x => x.MemberId == memberId);
            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(x => x.CheckInTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (items, totalCount);
        }

        public async Task<List<CheckIn>> GetActiveSessionsAsync()
        {
            return await _db.CheckIns
                .Where(x => x.CheckOutTime == null)
                .Include(x => x.Member)
                .OrderBy(x => x.CheckInTime)
                .ToListAsync();
        }

        public async Task<int> CountCheckInsBetweenAsync(DateTime from, DateTime to)
        {
            return await _db.CheckIns.CountAsync(x => x.CheckInTime >= from && x.CheckInTime < to);
        }

        public async Task<int> CountCheckOutsBetweenAsync(DateTime from, DateTime to)
        {
            return await _db.CheckIns.CountAsync(x =>
                x.CheckOutTime != null && x.CheckOutTime >= from && x.CheckOutTime < to);
        }

        public async Task<int> CountCurrentlyInsideAsync()
        {
            return await _db.CheckIns.CountAsync(x => x.CheckOutTime == null);
        }

        public async Task<List<CheckIn>> GetRecentAsync(int count)
        {
            return await _db.CheckIns
                .Include(x => x.Member)
                .OrderByDescending(x => x.CheckOutTime ?? x.CheckInTime)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<AttendanceRow>> GetForExportAsync(DateTime? from, DateTime? to)
        {
            var query = _db.CheckIns.AsQueryable();
            if (from.HasValue) query = query.Where(x => x.CheckInTime >= from.Value.Date);
            // `to` is inclusive of the whole day the caller picked.
            if (to.HasValue) query = query.Where(x => x.CheckInTime < to.Value.Date.AddDays(1));

            return await query
                .OrderByDescending(x => x.CheckInTime)
                .Select(x => new AttendanceRow
                {
                    Id = x.Id,
                    MemberName = x.Member.FullName,
                    MemberPhone = x.Member.PhoneNumber,
                    CheckInTime = x.CheckInTime,
                    CheckOutTime = x.CheckOutTime,
                    MinutesInside = x.CheckOutTime.HasValue
                        ? (int?)(x.CheckOutTime.Value - x.CheckInTime).TotalMinutes
                        : null,
                    Method = x.Method,
                    DeviceName = x.Device != null ? x.Device.Name : null,
                    Notes = x.Notes,
                })
                .ToListAsync();
        }
    }
}
