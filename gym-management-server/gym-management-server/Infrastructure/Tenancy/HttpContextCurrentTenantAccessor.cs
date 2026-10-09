namespace gym_management_server.Infrastructure.Tenancy
{
    /// <summary>Reads the tenant from the current request's JWT claims.</summary>
    public class HttpContextCurrentTenantAccessor : ICurrentTenantAccessor
    {
        /// <summary>Not one of the standard <see cref="System.Security.Claims.ClaimTypes"/> - this is our own.</summary>
        public const string TenantIdClaimType = "tenant_id";

        private readonly IHttpContextAccessor _httpContextAccessor;
        private Guid? _override;

        public HttpContextCurrentTenantAccessor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid? TenantId
        {
            get
            {
                if (_override.HasValue) return _override;
                var raw = _httpContextAccessor.HttpContext?.User?.FindFirst(TenantIdClaimType)?.Value;
                return Guid.TryParse(raw, out var id) ? id : null;
            }
        }

        public void SetTenant(Guid tenantId) => _override = tenantId;
    }
}
