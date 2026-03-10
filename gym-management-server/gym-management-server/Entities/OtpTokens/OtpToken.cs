using gym_management_server.Entities.Users;

namespace gym_management_server.Entities.OtpTokens
{
    public class OtpToken
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public string OtpCode { get; set; } = null!;
        public byte Purpose { get; set; } = 0; // 0 = ForgotPassword
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
