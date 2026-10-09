namespace gym_management_server.Entities.Tenants
{
    /// <summary>
    /// Implemented by every entity scoped to one gym branch (docs/ImprovementPlan.md mục 1).
    /// GymManagementContext stamps <see cref="TenantId"/> onto every newly-added row of these types
    /// automatically (from the ambient <c>ICurrentTenantAccessor</c>) and applies a matching global
    /// query filter, so most services never touch TenantId directly - see
    /// GymManagementContext.StampTenantId/OnModelCreating.
    /// </summary>
    public interface ITenantScoped
    {
        Guid TenantId { get; set; }
    }
}
