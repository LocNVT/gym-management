using gym_management_server.DTOs.Export;
using gym_management_server.Entities.Devices;

namespace gym_management_server.Repositories.Devices
{
    public interface IAttendanceDeviceRepository
    {
        Task<AttendanceDevice?> GetByIdAsync(Guid id);
        Task<List<AttendanceDevice>> GetAllAsync();
        Task<(List<AttendanceDevice> Items, int TotalCount)> GetPagedAsync(int page, int pageSize);
        Task AddAsync(AttendanceDevice device);
        Task UpdateAsync(AttendanceDevice device);
        Task DeleteAsync(Guid id);

        /// <summary>All non-deleted devices, ordered by name, projected for Excel export.</summary>
        Task<List<AttendanceDeviceRow>> GetForExportAsync();
    }
}
