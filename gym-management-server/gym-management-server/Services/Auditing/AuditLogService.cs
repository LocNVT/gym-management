using gym_management_server.DTOs.Auditing;
using gym_management_server.DTOs.Common;
using gym_management_server.Entities.Auditing;
using gym_management_server.Repositories.Auditing;

namespace gym_management_server.Services.Auditing
{
    public class AuditLogService
    {
        private readonly IAuditLogRepository _repository;
        private readonly GymManagementServiceMapObjects _mapObjects;

        public AuditLogService(IAuditLogRepository repository, GymManagementServiceMapObjects mapObjects)
        {
            _repository = repository;
            _mapObjects = mapObjects;
        }

        public async Task<PagedResult<AuditLogOutput>> QueryAsync(
            string? entityName, Guid? actorUserId, DateTime? from, DateTime? to, int page = 1, int pageSize = 20)
        {
            var (items, totalCount) = await _repository.QueryAsync(entityName, actorUserId, from, to, page, pageSize);
            var mapped = items.Select(x => _mapObjects.MapObjects<AuditLog, AuditLogOutput>(x)).ToList();
            return new PagedResult<AuditLogOutput> { Items = mapped, TotalCount = totalCount, Page = page, PageSize = pageSize };
        }
    }
}
