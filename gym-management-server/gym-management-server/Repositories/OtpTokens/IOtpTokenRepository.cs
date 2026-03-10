using gym_management_server.Entities.OtpTokens;

namespace gym_management_server.Repositories.OtpTokens
{
    public interface IOtpTokenRepository
    {
        Task AddAsync(OtpToken otpToken);
        Task<OtpToken?> GetValidOtpAsync(Guid userId, string otpCode, byte purpose);
        Task UpdateAsync(OtpToken otpToken);
    }
}
