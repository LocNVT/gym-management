namespace gym_management_server.DTOs.Fingerprints
{
    /// <summary>
    /// Enrolment / update payload. <see cref="CapturedTemplate"/> is the base64-encoded template
    /// produced by the scanner SDK — NEVER a raw image. It is encrypted before persistence.
    /// </summary>
    public class FingerprintTemplateInput
    {
        public Guid MemberId { get; set; }
        public byte FingerPosition { get; set; }

        /// <summary>Base64-encoded template bytes from the device SDK.</summary>
        public string CapturedTemplate { get; set; } = null!;

        /// <summary>Vendor/format of the template (defaults to "Mock" for hardware-free testing).</summary>
        public string Vendor { get; set; } = "Mock";

        public byte Quality { get; set; }
    }
}
