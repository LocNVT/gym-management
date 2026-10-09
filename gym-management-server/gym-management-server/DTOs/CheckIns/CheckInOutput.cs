using gym_management_server.Entities.Enums;
using gym_management_server.Entities.Members;

namespace gym_management_server.Entities.CheckIns
{
    public class CheckInOutput
    {
        public Guid Id { get; set; }
        public Guid MemberId { get; set; }
        public Member Member { get; set; } = null!;
        public DateTime CheckInTime { get; set; } = DateTime.UtcNow;
        public CheckInMethod Method { get; set; } = CheckInMethod.Unknown;
        public DateTime? CheckOutTime { get; set; }
        public CheckOutMethod CheckOutMethod { get; set; } = CheckOutMethod.None;
        public string? Notes { get; set; }
    }
}
