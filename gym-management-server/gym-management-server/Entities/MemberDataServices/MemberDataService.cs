using gym_management_server.Entities.Members;
using gym_management_server.Entities.ServicePackages;

namespace gym_management_server.Entities.MemberDataServices
{
    public class MemberDataService
    {
        public Guid Id { get; set; }
        public Guid MemberId { get; set; }
        public Member Member { get; set; } = null!;
        public Guid ServicePackageId { get; set; }
        public ServicePackage ServicePackage { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal PriceAtPurchase { get; set; }
        public int? RemainingCheckins { get; set; }
        public byte Status { get; set; } = 1;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
