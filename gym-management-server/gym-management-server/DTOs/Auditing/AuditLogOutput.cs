using gym_management_server.Entities.Enums;

namespace gym_management_server.DTOs.Auditing
{
    public class AuditLogOutput
    {
        public Guid Id { get; set; }
        public string EntityName { get; set; } = null!;
        public string EntityId { get; set; } = null!;
        public AuditAction Action { get; set; }
        public Guid? ActorUserId { get; set; }
        public string? ActorUsername { get; set; }
        public DateTime Timestamp { get; set; }
        public string? OldValuesJson { get; set; }
        public string? NewValuesJson { get; set; }
    }
}
