using gym_management_server.DTOs.Common;
using gym_management_server.DTOs.Devices;
using gym_management_server.Entities.Devices;
using gym_management_server.Infrastructure.Excel;
using gym_management_server.Repositories.Devices;

namespace gym_management_server.Services.Devices
{
    public class AttendanceDeviceService
    {
        private readonly IAttendanceDeviceRepository _repository;
        private readonly GymManagementServiceMapObjects _mapObjects;

        public AttendanceDeviceService(IAttendanceDeviceRepository repository, GymManagementServiceMapObjects mapObjects)
        {
            _repository = repository;
            _mapObjects = mapObjects;
        }

        public async Task<PagedResult<AttendanceDeviceOutput>> GetAllAsync(int page = 1, int pageSize = 10)
        {
            var (items, totalCount) = await _repository.GetPagedAsync(page, pageSize);
            var mapped = items.Select(d => _mapObjects.MapObjects<AttendanceDevice, AttendanceDeviceOutput>(d)).ToList();
            return new PagedResult<AttendanceDeviceOutput> { Items = mapped, TotalCount = totalCount, Page = page, PageSize = pageSize };
        }

        public async Task<AttendanceDeviceOutput?> GetByIdAsync(Guid id)
        {
            var device = await _repository.GetByIdAsync(id);
            return device == null ? null : _mapObjects.MapObjects<AttendanceDevice, AttendanceDeviceOutput>(device);
        }

        public async Task<AttendanceDeviceOutput> CreateAsync(AttendanceDeviceInput input)
        {
            var device = new AttendanceDevice
            {
                Id = Guid.NewGuid(),
                Name = input.Name,
                Location = input.Location,
                Vendor = input.Vendor,
                SerialNumber = input.SerialNumber,
                IsActive = input.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(device);
            return _mapObjects.MapObjects<AttendanceDevice, AttendanceDeviceOutput>(device);
        }

        public async Task<AttendanceDeviceOutput?> UpdateAsync(Guid id, AttendanceDeviceInput input)
        {
            var device = await _repository.GetByIdAsync(id);
            if (device == null) return null;

            device.Name = input.Name;
            device.Location = input.Location;
            device.Vendor = input.Vendor;
            device.SerialNumber = input.SerialNumber;
            device.IsActive = input.IsActive;
            device.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(device);
            return _mapObjects.MapObjects<AttendanceDevice, AttendanceDeviceOutput>(device);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var device = await _repository.GetByIdAsync(id);
            if (device == null) return false;

            await _repository.DeleteAsync(id);
            return true;
        }

        public async Task<byte[]> ExportAsync() =>
            ExcelWriter.Write(AttendanceDeviceSheet.Export, await _repository.GetForExportAsync());
    }
}
