namespace gym_management_server.Fingerprints
{
    /// <summary>
    /// Resolves a registered <see cref="IFingerprintProvider"/> by its vendor key. All providers are
    /// registered in DI; this factory just indexes them, so adding a new vendor is a one-line DI change
    /// with no edits to business logic.
    /// </summary>
    public class FingerprintProviderFactory : IFingerprintProviderFactory
    {
        private readonly Dictionary<string, IFingerprintProvider> _providers;

        public FingerprintProviderFactory(IEnumerable<IFingerprintProvider> providers)
        {
            _providers = providers.ToDictionary(p => p.Vendor, StringComparer.OrdinalIgnoreCase);
        }

        public IFingerprintProvider GetProvider(string vendor)
        {
            if (string.IsNullOrWhiteSpace(vendor))
                throw new ArgumentException("Vendor is required to resolve a fingerprint provider.", nameof(vendor));

            if (_providers.TryGetValue(vendor, out var provider))
                return provider;

            throw new NotSupportedException(
                $"No fingerprint provider is registered for vendor '{vendor}'. " +
                $"Registered vendors: {string.Join(", ", _providers.Keys)}.");
        }
    }
}
