using gym_management_server.Data.EntityFramework;
using gym_management_server.Entities.Auditing;
using Microsoft.EntityFrameworkCore;

namespace gym_management_server.Repositories.Auditing
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly GymManagementContext _db;

        public AuditLogRepository(GymManagementContext db)
        {
            _db = db;
        }

        public async Task<(List<AuditLog> Items, int TotalCount)> QueryAsync(
            string? entityName, Guid? actorUserId, DateTime? from, DateTime? to, int page, int pageSize)
        {
            var query = _db.AuditLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(entityName))
                query = query.Where(x => x.EntityName == entityName);
            if (actorUserId.HasValue)
                query = query.Where(x => x.ActorUserId == actorUserId.Value);
            if (from.HasValue)
                query = query.Where(x => x.Timestamp >= from.Value);
            if (to.HasValue)
                query = query.Where(x => x.Timestamp < to.Value);

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(x => x.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }
    }
}
