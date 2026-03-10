using gym_management_server.Entities.Members;

namespace gym_management_server.Entities.CheckIns
{
    public class CheckIn
    {
        public CheckIn(Guid id, Guid memberId, DateTime checkInTime, byte method, string? notes)
        {
            Id = id;
            MemberId = memberId;
            CheckInTime = checkInTime;
            Method = method;
            Notes = notes;
        }

        public Guid Id { get; set; }
        public Guid MemberId { get; set; }
        public Member Member { get; set; } = null!;
        public DateTime CheckInTime { get; set; } = DateTime.UtcNow;
        public byte Method { get; set; } = 0;
        public string? Notes { get; set; }
    }
}
