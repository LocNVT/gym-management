namespace gym_management_server.Fingerprints.Providers
{
    /// <summary>
    /// Placeholder for Suprema hardware integration. Wire the Suprema SDK into the methods below;
    /// routing happens automatically when a device's Vendor is "Suprema".
    /// </summary>
    public class SupremaFingerprintProvider : IFingerprintProvider
    {
        public string Vendor => "Suprema";

        public byte[] CreateTemplate(byte[] capturedTemplate)
            => throw new NotImplementedException("Suprema template creation is not yet integrated.");

        public FingerprintMatchResult Identify(byte[] probeTemplate, IReadOnlyList<FingerprintCandidate> candidates)
            => throw new NotImplementedException("Suprema matching is not yet integrated.");
    }
}
