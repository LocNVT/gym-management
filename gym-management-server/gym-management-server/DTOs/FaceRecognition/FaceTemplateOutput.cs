namespace gym_management_server.DTOs.FaceRecognition
{
    /// <summary>
    /// Read model for a registered face template. Deliberately OMITS the template bytes -
    /// biometric data is never returned over the API.
    /// </summary>
    public class FaceTemplateOutput
    {
        public Guid Id { get; set; }
        public Guid MemberId { get; set; }
        public string Vendor { get; set; } = null!;
        public byte Quality { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
