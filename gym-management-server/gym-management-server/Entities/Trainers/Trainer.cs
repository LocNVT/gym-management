namespace gym_management_server.Entities.Trainers
{
    public class Trainer
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Specialty { get; set; }
        public decimal HourlyRate { get; set; }
        public byte Status { get; set; } = 0;
        public string? Notes { get; set; }
    }
}
