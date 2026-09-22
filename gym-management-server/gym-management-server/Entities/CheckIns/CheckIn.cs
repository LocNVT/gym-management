using gym_management_server.Entities.Devices;
using gym_management_server.Entities.Enums;
using gym_management_server.Entities.Members;

namespace gym_management_server.Entities.CheckIns
{
    /// <summary>
    /// An attendance session for a member. A row with <see cref="CheckOutTime"/> == null is an
    /// "active" attendance (member is currently inside). The next successful match closes it (check-out).
    /// </summary>
    public class CheckIn
    {
        public CheckIn() { }

        public CheckIn(Guid id, Guid memberId, DateTime checkInTime, CheckInMethod method, string? notes)
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

        /// <summary>Null while the member is still inside; set on check-out.</summary>
        public DateTime? CheckOutTime { get; set; }

        /// <summary>0 = unknown/manual, 1 = card, 2 = fingerprint, ... (matches CheckInMethod).</summary>
        public CheckInMethod Method { get; set; } = CheckInMethod.Unknown;

        /// <summary>The device that recorded this attendance, if any (e.g. the fingerprint scanner).</summary>
        public Guid? DeviceId { get; set; }
        public AttendanceDevice? Device { get; set; }

        /// <summary>Staff user who operated the device / assisted, if applicable.</summary>
        public Guid? OperatorUserId { get; set; }

        public string? Notes { get; set; }
    }
}
