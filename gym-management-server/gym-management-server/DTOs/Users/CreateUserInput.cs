namespace gym_management_server.DTOs.Users
{
    /// <summary>Admin adding a staff account to their own tenant - see docs/ImprovementPlan.md mục 1.</summary>
    public class CreateUserInput
    {
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string FullName { get; set; } = null!;

        /// <summary>0 = Staff, 1 = Admin.</summary>
        public byte Role { get; set; } = 0;
    }

    public class SetUserActiveInput
    {
        public bool IsActive { get; set; }
    }

    public class SetUserRoleInput
    {
        /// <summary>0 = Staff, 1 = Admin.</summary>
        public byte Role { get; set; }
    }
}
