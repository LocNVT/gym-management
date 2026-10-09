namespace gym_management_server.DTOs.FaceRecognition
{
    /// <summary>A scan submitted by a device for identification. The matched member is then checked in or out.</summary>
    public class VerifyFaceInput
    {
        /// <summary>The device that performed the scan. Its Vendor selects the matching provider.</summary>
        public Guid DeviceId { get; set; }

        /// <summary>Base64-encoded probe template/embedding captured at the scanner.</summary>
        public string CapturedTemplate { get; set; } = null!;

        /// <summary>Optional staff user operating the device.</summary>
        public Guid? OperatorUserId { get; set; }
    }
}
