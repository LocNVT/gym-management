namespace gym_management_server.Fingerprints
{
    /// <summary>
    /// A candidate fingerprint template to match against, paired with the member it belongs to.
    /// Template bytes here are already DECRYPTED (the service decrypts before matching).
    /// </summary>
    public record FingerprintCandidate(Guid MemberId, Guid TemplateId, byte[] Template);

    /// <summary>Result of an identify/match operation.</summary>
    public record FingerprintMatchResult(bool Matched, Guid? MemberId, Guid? TemplateId, int Score);

    /// <summary>
    /// Vendor-neutral contract for fingerprint hardware/SDK operations. Business logic depends
    /// ONLY on this interface; concrete vendor implementations (ZKTeco, Suprema, DigitalPersona)
    /// are resolved at runtime via <see cref="IFingerprintProviderFactory"/> from a device's Vendor.
    /// </summary>
    public interface IFingerprintProvider
    {
        /// <summary>Vendor key this provider handles (case-insensitive), e.g. "ZKTeco", "Mock".</summary>
        string Vendor { get; }

        /// <summary>
        /// Normalises a captured payload into a storable template. For most SDKs the captured bytes
        /// are already a template; this hook lets a vendor post-process/validate before persistence.
        /// Never returns or accepts a raw image — callers pass templates only.
        /// </summary>
        byte[] CreateTemplate(byte[] capturedTemplate);

        /// <summary>
        /// Compares a probe template against a set of candidates and returns the best match
        /// above the provider's threshold, or a non-match result.
        /// </summary>
        FingerprintMatchResult Identify(byte[] probeTemplate, IReadOnlyList<FingerprintCandidate> candidates);
    }

    /// <summary>Resolves the correct <see cref="IFingerprintProvider"/> for a given vendor key.</summary>
    public interface IFingerprintProviderFactory
    {
        IFingerprintProvider GetProvider(string vendor);
    }
}
