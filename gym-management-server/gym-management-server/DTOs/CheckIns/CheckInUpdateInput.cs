namespace gym_management_server.Entities.CheckIns
{
    public class CheckInUpdateInput
    {
        public Guid MemberId { get; set; }
        public DateTime CheckInTime { get; set; } = DateTime.UtcNow;
        public byte Method { get; set; } = 0;
        public string? Notes { get; set; }
    }
}
