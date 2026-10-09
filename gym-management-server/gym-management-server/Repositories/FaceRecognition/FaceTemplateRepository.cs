using gym_management_server.Data.EntityFramework;
using gym_management_server.Entities.FaceRecognition;
using Microsoft.EntityFrameworkCore;

namespace gym_management_server.Repositories.FaceRecognition
{
    public class FaceTemplateRepository : IFaceTemplateRepository
    {
        private readonly GymManagementContext _db;

        public FaceTemplateRepository(GymManagementContext db) => _db = db;

        public async Task<FaceTemplate?> GetByIdAsync(Guid id)
        {
            return await _db.FaceTemplates.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        }

        public async Task<List<FaceTemplate>> GetByMemberAsync(Guid memberId)
        {
            return await _db.FaceTemplates
                .Where(x => x.MemberId == memberId && !x.IsDeleted)
                .ToListAsync();
        }

        public async Task<List<FaceTemplate>> GetAllActiveAsync()
        {
            return await _db.FaceTemplates.Where(x => !x.IsDeleted).ToListAsync();
        }

        public async Task AddAsync(FaceTemplate template)
        {
            _db.FaceTemplates.Add(template);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateAsync(FaceTemplate template)
        {
            _db.FaceTemplates.Update(template);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var template = await _db.FaceTemplates.FirstOrDefaultAsync(x => x.Id == id);
            if (template != null)
            {
                template.IsDeleted = true; // soft delete
                template.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }
    }
}
