using gym_management_server.Entities.FaceRecognition;

namespace gym_management_server.Repositories.FaceRecognition
{
    public interface IFaceTemplateRepository
    {
        Task<FaceTemplate?> GetByIdAsync(Guid id);
        Task<List<FaceTemplate>> GetByMemberAsync(Guid memberId);

        /// <summary>All active templates across all members - the candidate set for identification.</summary>
        Task<List<FaceTemplate>> GetAllActiveAsync();

        Task AddAsync(FaceTemplate template);
        Task UpdateAsync(FaceTemplate template);
        Task DeleteAsync(Guid id);
    }
}
