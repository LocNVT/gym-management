using gym_management_server.Entities.Members;

namespace gym_management_server.Entities.MemberDataServices
{
    public class MemberDataServiceInput
    {
        public Guid Id { get; set; }
        public Guid MemberId { get; set; }
        public Guid ServicePackageId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal PriceAtPurchase { get; set; }
        public int? RemainingCheckins { get; set; }
        public byte Status { get; set; } = 1;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
