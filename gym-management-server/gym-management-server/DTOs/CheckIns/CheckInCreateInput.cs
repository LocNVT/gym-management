using gym_management_server.Entities.Enums;

namespace gym_management_server.Entities.CheckIns
{
    public class CheckInCreateInput
    {
        public Guid MemberId { get; set; }
        public DateTime CheckInTime { get; set; } = DateTime.UtcNow;
        public CheckInMethod Method { get; set; } = CheckInMethod.Unknown;
        public string? Notes { get; set; }
    }
}
