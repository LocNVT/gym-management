namespace gym_management_server.Entities.Users
{
    public class User
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? PasswordHash { get; set; }
        public string FullName { get; set; } = null!;
        public byte Role { get; set; } = 0; // 0 = Staff, 1 = Admin
        public string? GoogleId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
