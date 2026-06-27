namespace gym_management_server.DTOs.CheckIns
{
    /// <summary>A member who is currently checked in (open session).</summary>
    public class ActiveAttendanceOutput
    {
        public Guid CheckInId { get; set; }
        public Guid MemberId { get; set; }
        public string? MemberName { get; set; }
        public DateTime CheckInTime { get; set; }
        public int MinutesInside { get; set; }
        public Guid? DeviceId { get; set; }
    }

    /// <summary>Headline counts for a given day.</summary>
    public class AttendanceSummaryOutput
    {
        public DateTime Date { get; set; }
        public int CheckInsToday { get; set; }
        public int CheckOutsToday { get; set; }
        public int CurrentlyInside { get; set; }
    }

    /// <summary>A recent attendance event for the activity feed.</summary>
    public class AttendanceEventOutput
    {
        public Guid CheckInId { get; set; }
        public Guid MemberId { get; set; }
        public string? MemberName { get; set; }
        /// <summary>"check-in" or "check-out".</summary>
        public string Action { get; set; } = "check-in";
        public DateTime Timestamp { get; set; }
    }
}
