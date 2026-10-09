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

        /// <summary>
        /// Looks a device up WITHOUT the tenant query filter. Only for the anonymous
        /// fingerprint-scan flow (FingerprintService.VerifyAsync), which has no JWT/tenant yet and
        /// must resolve the device's OWN tenant before anything else can be scoped correctly.
        /// </summary>
        Task<AttendanceDevice?> GetByIdIgnoringTenantAsync(Guid id);
    }
}
