namespace gym_management_server.DTOs.FaceRecognition
{
    /// <summary>
    /// Enrolment / update payload. <see cref="CapturedTemplate"/> is the base64-encoded
    /// template/embedding produced by the face SDK - NEVER a raw image. It is encrypted before
    /// persistence, same as fingerprint templates.
    /// </summary>
    public class FaceTemplateInput
    {
        public Guid MemberId { get; set; }

        /// <summary>Base64-encoded template/embedding bytes from the device SDK.</summary>
        public string CapturedTemplate { get; set; } = null!;

        /// <summary>Vendor/format of the template (defaults to "Mock" for hardware-free testing).</summary>
        public string Vendor { get; set; } = "Mock";

        public byte Quality { get; set; }
    }
}
