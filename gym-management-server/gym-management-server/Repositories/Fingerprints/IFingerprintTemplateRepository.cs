using gym_management_server.Entities.Fingerprints;

namespace gym_management_server.Repositories.Fingerprints
{
    public interface IFingerprintTemplateRepository
    {
        Task<FingerprintTemplate?> GetByIdAsync(Guid id);
        Task<List<FingerprintTemplate>> GetByMemberAsync(Guid memberId);

        /// <summary>All active templates across all members — the candidate set for identification.</summary>
        Task<List<FingerprintTemplate>> GetAllActiveAsync();

        Task AddAsync(FingerprintTemplate template);
        Task UpdateAsync(FingerprintTemplate template);
        Task DeleteAsync(Guid id);
    }
}
