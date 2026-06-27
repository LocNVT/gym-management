namespace gym_management_server.Fingerprints.Providers
{
    /// <summary>
    /// A deterministic, hardware-free provider used for development, demos and automated tests.
    /// "Templates" are arbitrary byte arrays; similarity is the fraction of matching bytes, scored 0-100.
    /// This lets the full enrol -> verify -> check-in/out flow be exercised without a physical scanner.
    /// </summary>
    public class MockFingerprintProvider : IFingerprintProvider
    {
        /// <summary>Minimum score (0-100) to accept a match.</summary>
        public const int MatchThreshold = 60;

        public string Vendor => "Mock";

        public byte[] CreateTemplate(byte[] capturedTemplate)
        {
            if (capturedTemplate == null || capturedTemplate.Length == 0)
                throw new ArgumentException("Captured template is empty.", nameof(capturedTemplate));
            return capturedTemplate;
        }

        public FingerprintMatchResult Identify(byte[] probeTemplate, IReadOnlyList<FingerprintCandidate> candidates)
        {
            FingerprintMatchResult best = new(false, null, null, 0);

            foreach (var candidate in candidates)
            {
                var score = Similarity(probeTemplate, candidate.Template);
                if (score > best.Score)
                    best = new FingerprintMatchResult(score >= MatchThreshold, candidate.MemberId, candidate.TemplateId, score);
            }

            return best.Score >= MatchThreshold
                ? best with { Matched = true }
                : new FingerprintMatchResult(false, best.MemberId, best.TemplateId, best.Score);
        }

        private static int Similarity(byte[] a, byte[] b)
        {
            if (a.Length == 0 || b.Length == 0) return 0;
            var length = Math.Min(a.Length, b.Length);
            var matching = 0;
            for (var i = 0; i < length; i++)
                if (a[i] == b[i]) matching++;

            // Penalise length mismatch by scoring over the longer array.
            var denominator = Math.Max(a.Length, b.Length);
            return (int)Math.Round(matching * 100.0 / denominator);
        }
    }
}
