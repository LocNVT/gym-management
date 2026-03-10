using gym_management_server.Entities.Members;

namespace gym_management_server.Entities.CheckIns
{
    public class CheckInOutput
    {
        public Guid Id { get; set; }
        public Guid MemberId { get; set; }
        public Member Member { get; set; } = null!;
        public DateTime CheckInTime { get; set; } = DateTime.UtcNow;
        public byte Method { get; set; } = 0;
        public string? Notes { get; set; }
    }
}
