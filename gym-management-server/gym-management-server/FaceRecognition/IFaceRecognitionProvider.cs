namespace gym_management_server.FaceRecognition
{
    /// <summary>
    /// A candidate face template to match against, paired with the member it belongs to.
    /// Template bytes here are already DECRYPTED (the service decrypts before matching).
    /// </summary>
    public record FaceCandidate(Guid MemberId, Guid TemplateId, byte[] Template);

    /// <summary>Result of an identify/match operation.</summary>
    public record FaceMatchResult(bool Matched, Guid? MemberId, Guid? TemplateId, int Score);

    /// <summary>
    /// Vendor-neutral contract for face-recognition hardware/SDK operations - mirrors
    /// Fingerprints/IFingerprintProvider.cs by design (see docs/ImprovementPlan.md mục 5). Business
    /// logic depends ONLY on this interface; a concrete vendor SDK is resolved at runtime via
    /// <see cref="IFaceRecognitionProviderFactory"/> from a device's Vendor.
    ///
    /// Unlike fingerprint minutiae, a real vendor's "template" here is typically an embedding
    /// vector from a neural net, and Identify would compare via cosine similarity rather than
    /// byte-matching - that's entirely inside the vendor implementation; this interface doesn't
    /// need to know or care. Liveness detection (rejecting a photo-of-a-photo) is also expected to
    /// be the vendor SDK/device's responsibility, not something this interface models.
    /// </summary>
    public interface IFaceRecognitionProvider
    {
        /// <summary>Vendor key this provider handles (case-insensitive), e.g. "Mock".</summary>
        string Vendor { get; }

        /// <summary>
        /// Normalises a captured payload (e.g. an SDK-produced embedding) into a storable template.
        /// Never returns or accepts a raw image - callers pass templates/embeddings only.
        /// </summary>
        byte[] CreateTemplate(byte[] capturedTemplate);

        /// <summary>
        /// Compares a probe template against a set of candidates and returns the best match
        /// above the provider's threshold, or a non-match result.
        /// </summary>
        FaceMatchResult Identify(byte[] probeTemplate, IReadOnlyList<FaceCandidate> candidates);
    }

    /// <summary>Resolves the correct <see cref="IFaceRecognitionProvider"/> for a given vendor key.</summary>
    public interface IFaceRecognitionProviderFactory
    {
        IFaceRecognitionProvider GetProvider(string vendor);
    }
}
