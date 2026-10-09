using gym_management_server.Entities.Enums;
using gym_management_server.Entities.Tenants;

namespace gym_management_server.Entities.Trainers
{
    public class Trainer : ITenantScoped
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string FullName { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Specialty { get; set; }
        public decimal HourlyRate { get; set; }
        public TrainerStatus Status { get; set; } = TrainerStatus.Working;
        public string? Notes { get; set; }

        public byte[]? RowVersion { get; set; }
    }
}
