namespace gym_management_server.DTOs.FaceRecognition
{
    /// <summary>Outcome of a verify request: whether a member matched and what attendance action occurred.</summary>
    public class VerifyFaceResult
    {
        public bool Matched { get; set; }
        public int Score { get; set; }

        public Guid? MemberId { get; set; }
        public string? MemberName { get; set; }

        /// <summary>"check-in", "check-out", or "none" (no match).</summary>
        public string Action { get; set; } = "none";

        public Guid? CheckInId { get; set; }
        public DateTime? Timestamp { get; set; }
    }
}
