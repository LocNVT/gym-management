namespace gym_management_server.DTOs.Auth
{
    public class AuthOutput
    {
        public string Token { get; set; } = null!;
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public byte Role { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
