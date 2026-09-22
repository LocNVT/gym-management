using gym_management_server.DTOs.Common;
using gym_management_server.Entities.CheckIns;
using gym_management_server.Infrastructure.Excel;
using gym_management_server.Repositories.CheckIns;

namespace gym_management_server.Services.CheckIns
{
    public class CheckInService
    {
        private readonly ICheckInRepository _checkInRepository;
        private readonly GymManagementServiceMapObjects _mapObjects;

        public CheckInService(GymManagementServiceMapObjects mapObjects, ICheckInRepository checkInRepository)
        {
            _mapObjects = mapObjects;
            _checkInRepository = checkInRepository;
        }

        public async Task<CheckInOutput> GetAsync(Guid id)
        {
            var checkIn = await _checkInRepository.GetByIdAsync(id);
            if (checkIn == null)
            {
                return new CheckInOutput();
            }
            return _mapObjects.MapObjects<CheckIn, CheckInOutput>(checkIn);
        }

        public async Task<PagedResult<CheckInOutput>> GetListAsync(int page = 1, int pageSize = 10)
        {
            var (items, totalCount) = await _checkInRepository.GetPagedAsync(page, pageSize);
            var mapped = items.Select(c => _mapObjects.MapObjects<CheckIn, CheckInOutput>(c)).ToList();
            return new PagedResult<CheckInOutput> { Items = mapped, TotalCount = totalCount, Page = page, PageSize = pageSize };
        }

        public async Task<CheckInOutput> CreateAsync(CheckInCreateInput input)
        {
            var checkIn = new CheckIn(Guid.NewGuid(), input.MemberId, DateTime.Now, input.Method, input.Notes);
            await _checkInRepository.AddAsync(checkIn);
            return _mapObjects.MapObjects<CheckIn, CheckInOutput>(checkIn);
        }

        public async Task<CheckInOutput> UpdateAsync(Guid id, CheckInUpdateInput input)
        {
            var checkIn = await _checkInRepository.GetByIdAsync(id);
            if (checkIn == null)
            {
                return new CheckInOutput();
            }
            await _checkInRepository.UpdateAsync(checkIn);
            return _mapObjects.MapObjects<CheckIn, CheckInOutput>(checkIn);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var checkIn = await _checkInRepository.GetByIdAsync(id);
            if (checkIn == null) return false;

            await _checkInRepository.DeleteAsync(id);
            return true;
        }

        public async Task<byte[]> ExportAsync(DateTime? from, DateTime? to) =>
            ExcelWriter.Write(CheckInSheet.Export, await _checkInRepository.GetForExportAsync(from, to));
    }
}
