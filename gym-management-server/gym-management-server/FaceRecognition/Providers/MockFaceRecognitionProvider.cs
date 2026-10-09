namespace gym_management_server.FaceRecognition.Providers
{
    /// <summary>
    /// A deterministic, hardware-free provider used for development, demos and automated tests -
    /// mirrors Fingerprints/Providers/MockFingerprintProvider.cs. "Templates" are arbitrary byte
    /// arrays standing in for a real embedding vector; similarity is the fraction of matching
    /// bytes, scored 0-100. This lets the full enrol -> verify -> check-in/out flow be exercised
    /// before any real face-recognition SDK is integrated.
    /// </summary>
    public class MockFaceRecognitionProvider : IFaceRecognitionProvider
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

        public FaceMatchResult Identify(byte[] probeTemplate, IReadOnlyList<FaceCandidate> candidates)
        {
            FaceMatchResult best = new(false, null, null, 0);

            foreach (var candidate in candidates)
            {
                var score = Similarity(probeTemplate, candidate.Template);
                if (score > best.Score)
                    best = new FaceMatchResult(score >= MatchThreshold, candidate.MemberId, candidate.TemplateId, score);
            }

            return best.Score >= MatchThreshold
                ? best with { Matched = true }
                : new FaceMatchResult(false, best.MemberId, best.TemplateId, best.Score);
        }

        private static int Similarity(byte[] a, byte[] b)
        {
            if (a.Length == 0 || b.Length == 0) return 0;
            var length = Math.Min(a.Length, b.Length);
            var matching = 0;
            for (var i = 0; i < length; i++)
                if (a[i] == b[i]) matching++;

            var denominator = Math.Max(a.Length, b.Length);
            return (int)Math.Round(matching * 100.0 / denominator);
        }
    }
}
