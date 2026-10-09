using gym_management_server.Entities.Auditing;

namespace gym_management_server.Repositories.Auditing
{
    public interface IAuditLogRepository
    {
        /// <summary>Audit trail, newest first, optionally filtered. All filters are optional/AND-ed.</summary>
        Task<(List<AuditLog> Items, int TotalCount)> QueryAsync(
            string? entityName, Guid? actorUserId, DateTime? from, DateTime? to, int page, int pageSize);
    }
}
