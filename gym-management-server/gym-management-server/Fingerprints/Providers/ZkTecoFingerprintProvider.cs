namespace gym_management_server.Fingerprints.Providers
{
    /// <summary>
    /// Placeholder for ZKTeco hardware integration. Wire the ZKTeco SDK (template extraction +
    /// matching) into the methods below; the rest of the system already routes to this class
    /// whenever a device's Vendor is "ZKTeco".
    /// </summary>
    public class ZkTecoFingerprintProvider : IFingerprintProvider
    {
        public string Vendor => "ZKTeco";

        public byte[] CreateTemplate(byte[] capturedTemplate)
            => throw new NotImplementedException("ZKTeco template creation is not yet integrated.");

        public FingerprintMatchResult Identify(byte[] probeTemplate, IReadOnlyList<FingerprintCandidate> candidates)
            => throw new NotImplementedException("ZKTeco matching is not yet integrated.");
    }
}
