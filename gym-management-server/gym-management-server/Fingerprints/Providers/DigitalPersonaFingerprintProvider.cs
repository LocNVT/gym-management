namespace gym_management_server.Fingerprints.Providers
{
    /// <summary>
    /// Placeholder for DigitalPersona hardware integration. Wire the DigitalPersona SDK into the
    /// methods below; routing happens automatically when a device's Vendor is "DigitalPersona".
    /// </summary>
    public class DigitalPersonaFingerprintProvider : IFingerprintProvider
    {
        public string Vendor => "DigitalPersona";

        public byte[] CreateTemplate(byte[] capturedTemplate)
            => throw new NotImplementedException("DigitalPersona template creation is not yet integrated.");

        public FingerprintMatchResult Identify(byte[] probeTemplate, IReadOnlyList<FingerprintCandidate> candidates)
            => throw new NotImplementedException("DigitalPersona matching is not yet integrated.");
    }
}
