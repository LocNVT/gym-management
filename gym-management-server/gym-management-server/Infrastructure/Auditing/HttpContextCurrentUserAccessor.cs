using System.Security.Claims;

namespace gym_management_server.Infrastructure.Auditing
{
    /// <summary>Reads the acting user from the current request's JWT claims.</summary>
    public class HttpContextCurrentUserAccessor : ICurrentUserAccessor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HttpContextCurrentUserAccessor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid? UserId
        {
            get
            {
                var raw = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
                return Guid.TryParse(raw, out var id) ? id : null;
            }
        }

        public string? Username => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name);
    }

    /// <summary>No request in scope (e.g. a direct DbContext in a unit test or a background job).</summary>
    public sealed class NullCurrentUserAccessor : ICurrentUserAccessor
    {
        public static readonly NullCurrentUserAccessor Instance = new();
        private NullCurrentUserAccessor() { }

        public Guid? UserId => null;
        public string? Username => null;
    }
}
