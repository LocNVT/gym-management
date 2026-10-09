namespace gym_management_server.Infrastructure.Tenancy
{
    /// <summary>
    /// The tenant the current request is scoped to - see docs/ImprovementPlan.md mục 1.
    /// Normally resolved from the caller's JWT, but settable for the one flow that has no user JWT
    /// to read: an anonymous fingerprint-device scan, which instead derives its tenant from the
    /// device it names (see FingerprintService.VerifyAsync).
    /// </summary>
    public interface ICurrentTenantAccessor
    {
        Guid? TenantId { get; }

        /// <summary>Overrides the resolved tenant for the rest of this request/scope.</summary>
        void SetTenant(Guid tenantId);
    }

    /// <summary>
    /// Used when GymManagementContext is constructed without an explicit ICurrentTenantAccessor
    /// (every test that builds one directly, rather than through DI). Defaults to a freshly
    /// generated tenant rather than null specifically so that single-context test code - seed some
    /// rows, then query them back through the same context - keeps working unmodified: both the
    /// auto-stamp on save and the query filter land on the same random tenant for that context's
    /// lifetime. Never a shared singleton (unlike NullCurrentUserAccessor): each context that
    /// defaults to one gets its own instance, and GymManagementContext.CurrentTenant exposes it so
    /// a test can hand the exact same accessor to a service under test (see FingerprintServiceTests).
    /// </summary>
    public sealed class NullCurrentTenantAccessor : ICurrentTenantAccessor
    {
        private Guid? _tenantId = Guid.NewGuid();

        public Guid? TenantId => _tenantId;
        public void SetTenant(Guid tenantId) => _tenantId = tenantId;
    }
}
