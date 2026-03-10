using gym_management_server.DTOs.Common;
using gym_management_server.Entities.MemberDataServices;
using gym_management_server.Repositories.MemberDataServices;

namespace gym_management_server.Services.MemberDataServices
{
    public class MemberDataServiceService
    {
        private readonly IMemberDataServiceRepository _memberDataServiceRepository;
        private readonly GymManagementServiceMapObjects _mapObjects;

        public MemberDataServiceService(IMemberDataServiceRepository memberDataServiceRepository, GymManagementServiceMapObjects mapObjects)
        {
            _memberDataServiceRepository = memberDataServiceRepository;
            _mapObjects = mapObjects;
        }

        public async Task<PagedResult<MemberDataServiceOutput>> GetAllAsync(int page = 1, int pageSize = 10)
        {
            var (items, totalCount) = await _memberDataServiceRepository.GetPagedAsync(page, pageSize);
            var mapped = items.Select(m => _mapObjects.MapObjects<MemberDataService, MemberDataServiceOutput>(m)).ToList();
            return new PagedResult<MemberDataServiceOutput> { Items = mapped, TotalCount = totalCount, Page = page, PageSize = pageSize };
        }

        public async Task<MemberDataServiceOutput?> GetAsync(Guid id)
        {
            var memberDataService = await _memberDataServiceRepository.GetByIdAsync(id);
            if (memberDataService == null) { return null; }

            return _mapObjects.MapObjects<MemberDataService, MemberDataServiceOutput>(memberDataService);
        }

        public async Task<MemberDataServiceOutput> CreateAsync(MemberDataServiceInput input)
        {
            var memberDataService = new MemberDataService();
            memberDataService.Id = Guid.NewGuid();
            memberDataService.MemberId = input.MemberId;
            memberDataService.ServicePackageId = input.ServicePackageId;
            memberDataService.StartDate = input.StartDate;
            memberDataService.EndDate = input.EndDate;
            memberDataService.PriceAtPurchase = input.PriceAtPurchase;
            memberDataService.RemainingCheckins = input.RemainingCheckins;
            memberDataService.Status = input.Status;
            memberDataService.CreatedAt = DateTime.UtcNow;

            await _memberDataServiceRepository.AddAsync(memberDataService);
            return _mapObjects.MapObjects<MemberDataService, MemberDataServiceOutput>(memberDataService);
        }

        public async Task<MemberDataServiceOutput?> UpdateAsync(Guid id, MemberDataServiceInput input)
        {
            var memberDataService = await _memberDataServiceRepository.GetByIdAsync(id);
            if (memberDataService == null) return null;

            memberDataService.MemberId = input.MemberId;
            memberDataService.ServicePackageId = input.ServicePackageId;
            memberDataService.StartDate = input.StartDate;
            memberDataService.EndDate = input.EndDate;
            memberDataService.PriceAtPurchase = input.PriceAtPurchase;
            memberDataService.RemainingCheckins = input.RemainingCheckins;
            memberDataService.Status = input.Status;
            memberDataService.UpdatedAt = DateTime.UtcNow;

            await _memberDataServiceRepository.UpdateAsync(memberDataService);
            return _mapObjects.MapObjects<MemberDataService, MemberDataServiceOutput>(memberDataService);
        }

        public async Task<bool> DeleteMemberAsync(Guid id)
        {
            var member = await _memberDataServiceRepository.GetByIdAsync(id);
            if (member == null) return false;

            await _memberDataServiceRepository.DeleteAsync(id);
            return true;
        }
    }
}
