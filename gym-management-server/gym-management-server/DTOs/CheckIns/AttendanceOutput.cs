using gym_management_server.Entities.Enums;

namespace gym_management_server.DTOs.CheckIns
{
    /// <summary>An attendance session row for member history (check-in plus optional check-out).</summary>
    public class AttendanceOutput
    {
        public Guid Id { get; set; }
        public Guid MemberId { get; set; }
        public DateTime CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public CheckInMethod Method { get; set; }
        public Guid? DeviceId { get; set; }
        public Guid? OperatorUserId { get; set; }
        public string? Notes { get; set; }
    }
}
