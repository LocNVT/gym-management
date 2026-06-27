using gym_management_server.Data.EntityFramework;
using gym_management_server.Entities.Fingerprints;
using Microsoft.EntityFrameworkCore;

namespace gym_management_server.Repositories.Fingerprints
{
    public class FingerprintTemplateRepository : IFingerprintTemplateRepository
    {
        private readonly GymManagementContext _db;

        public FingerprintTemplateRepository(GymManagementContext db) => _db = db;

        public async Task<FingerprintTemplate?> GetByIdAsync(Guid id)
        {
            return await _db.FingerprintTemplates.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        }

        public async Task<List<FingerprintTemplate>> GetByMemberAsync(Guid memberId)
        {
            return await _db.FingerprintTemplates
                .Where(x => x.MemberId == memberId && !x.IsDeleted)
                .ToListAsync();
        }

        public async Task<List<FingerprintTemplate>> GetAllActiveAsync()
        {
            return await _db.FingerprintTemplates.Where(x => !x.IsDeleted).ToListAsync();
        }

        public async Task AddAsync(FingerprintTemplate template)
        {
            _db.FingerprintTemplates.Add(template);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateAsync(FingerprintTemplate template)
        {
            _db.FingerprintTemplates.Update(template);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var template = await _db.FingerprintTemplates.FirstOrDefaultAsync(x => x.Id == id);
            if (template != null)
            {
                template.IsDeleted = true; // soft delete
                template.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }
    }
}
