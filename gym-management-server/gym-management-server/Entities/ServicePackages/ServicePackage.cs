using gym_management_server.Entities.MemberDataServices;
using gym_management_server.Entities.Tenants;

namespace gym_management_server.Entities.ServicePackages
{
    public class ServicePackage : ITenantScoped
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int DurationDays { get; set; }
        public int? MaxCheckins { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public byte[]? RowVersion { get; set; }

        public List<MemberDataService> MemberDataServices { get; set; } = new();
    }
}
