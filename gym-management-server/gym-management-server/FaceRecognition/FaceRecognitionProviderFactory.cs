namespace gym_management_server.FaceRecognition
{
    /// <summary>
    /// Resolves a registered <see cref="IFaceRecognitionProvider"/> by its vendor key - mirrors
    /// FingerprintProviderFactory.cs. All providers are registered in DI; this factory just indexes
    /// them, so adding a real vendor SDK later is a one-line DI change with no edits to business logic.
    /// </summary>
    public class FaceRecognitionProviderFactory : IFaceRecognitionProviderFactory
    {
        private readonly Dictionary<string, IFaceRecognitionProvider> _providers;

        public FaceRecognitionProviderFactory(IEnumerable<IFaceRecognitionProvider> providers)
        {
            _providers = providers.ToDictionary(p => p.Vendor, StringComparer.OrdinalIgnoreCase);
        }

        public IFaceRecognitionProvider GetProvider(string vendor)
        {
            if (string.IsNullOrWhiteSpace(vendor))
                throw new ArgumentException("Vendor is required to resolve a face recognition provider.", nameof(vendor));

            if (_providers.TryGetValue(vendor, out var provider))
                return provider;

            throw new NotSupportedException(
                $"No face recognition provider is registered for vendor '{vendor}'. " +
                $"Registered vendors: {string.Join(", ", _providers.Keys)}.");
        }
    }
}
