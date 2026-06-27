namespace gym_management_server.DTOs.Trainers
{
    public class TrainerOutput
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Specialty { get; set; }
        public decimal HourlyRate { get; set; }
        public byte Status { get; set; }
        public string? Notes { get; set; }
    }
}
