using gym_management_server.Data.EntityFramework;
using gym_management_server.Entities.OtpTokens;
using Microsoft.EntityFrameworkCore;

namespace gym_management_server.Repositories.OtpTokens
{
    public class OtpTokenRepository : IOtpTokenRepository
    {
        private readonly GymManagementContext _db;

        public OtpTokenRepository(GymManagementContext db)
        {
            _db = db;
        }

        public async Task AddAsync(OtpToken otpToken)
        {
            _db.OtpTokens.Add(otpToken);
            await _db.SaveChangesAsync();
        }

        public async Task<OtpToken?> GetValidOtpAsync(Guid userId, string otpCode, byte purpose)
        {
            return await _db.OtpTokens.FirstOrDefaultAsync(x =>
                x.UserId == userId &&
                x.OtpCode == otpCode &&
                x.Purpose == purpose &&
                !x.IsUsed &&
                x.ExpiresAt > DateTime.UtcNow);
        }

        public async Task UpdateAsync(OtpToken otpToken)
        {
            _db.OtpTokens.Update(otpToken);
            await _db.SaveChangesAsync();
        }
    }
}
