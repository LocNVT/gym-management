using gym_management_server.Entities.Enums;
using gym_management_server.Entities.Tenants;

namespace gym_management_server.Entities.Auditing
{
    /// <summary>
    /// One row per create/update/delete on an audited entity (see
    /// GymManagementContext.AuditedEntities). Captured automatically in SaveChanges/SaveChangesAsync
    /// so no service or controller needs to remember to write one - see docs/ImprovementPlan.md mục 6.
    /// </summary>
    public class AuditLog : ITenantScoped
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        /// <summary>CLR type name of the changed entity, e.g. "Invoice".</summary>
        public string EntityName { get; set; } = null!;

        /// <summary>String form of the entity's primary key (Guid.ToString() for every entity today).</summary>
        public string EntityId { get; set; } = null!;

        public AuditAction Action { get; set; }

        /// <summary>Who made the change. Null for system/background actions (no HTTP request).</summary>
        public Guid? ActorUserId { get; set; }

        /// <summary>
        /// Denormalised alongside <see cref="ActorUserId"/> so the trail still reads if that user
        /// is later deleted.
        /// </summary>
        public string? ActorUsername { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>JSON property bag of the row's values before the change. Null for Create.</summary>
        public string? OldValuesJson { get; set; }

        /// <summary>JSON property bag of the row's values after the change. Null for Delete.</summary>
        public string? NewValuesJson { get; set; }
    }
}
