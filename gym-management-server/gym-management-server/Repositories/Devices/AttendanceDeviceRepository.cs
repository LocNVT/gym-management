using gym_management_server.Data.EntityFramework;
using gym_management_server.Entities.Devices;
using Microsoft.EntityFrameworkCore;

namespace gym_management_server.Repositories.Devices
{
    public class AttendanceDeviceRepository : IAttendanceDeviceRepository
    {
        private readonly GymManagementContext _db;

        public AttendanceDeviceRepository(GymManagementContext db) => _db = db;

        public async Task<AttendanceDevice?> GetByIdAsync(Guid id)
        {
            return await _db.AttendanceDevices.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        }

        public async Task<List<AttendanceDevice>> GetAllAsync()
        {
            return await _db.AttendanceDevices.Where(x => !x.IsDeleted).ToListAsync();
        }

        public async Task<(List<AttendanceDevice> Items, int TotalCount)> GetPagedAsync(int page, int pageSize)
        {
            var query = _db.AttendanceDevices.Where(x => !x.IsDeleted);
            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (items, totalCount);
        }

        public async Task AddAsync(AttendanceDevice device)
        {
            _db.AttendanceDevices.Add(device);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateAsync(AttendanceDevice device)
        {
            _db.AttendanceDevices.Update(device);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var device = await _db.AttendanceDevices.FirstOrDefaultAsync(x => x.Id == id);
            if (device != null)
            {
                device.IsDeleted = true; // soft delete
                device.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }
    }
}
