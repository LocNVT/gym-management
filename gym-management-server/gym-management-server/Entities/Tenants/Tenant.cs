namespace gym_management_server.Entities.Tenants
{
    /// <summary>
    /// One gym branch/location. The scoping unit for every other entity - see
    /// docs/ImprovementPlan.md mục 1. Created either via Auth/register (self-service: a new tenant
    /// plus its first Admin) or, for additional branches of an existing operator, directly by an
    /// operator with cross-tenant access (not yet modelled - today every tenant is independent).
    /// </summary>
    public class Tenant
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
