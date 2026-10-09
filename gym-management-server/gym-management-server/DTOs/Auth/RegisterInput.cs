namespace gym_management_server.DTOs.Auth
{
    public class RegisterInput
    {
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string FullName { get; set; } = null!;

        /// <summary>
        /// Name of the new gym branch this registration creates (self-service onboarding always
        /// creates a brand-new tenant with this account as its first Admin - see
        /// docs/ImprovementPlan.md mục 1). Falls back to a generic name if left blank.
        /// </summary>
        public string? TenantName { get; set; }
    }
}
