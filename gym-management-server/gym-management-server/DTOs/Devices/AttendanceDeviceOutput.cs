namespace gym_management_server.DTOs.Devices
{
    public class AttendanceDeviceOutput
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Location { get; set; }
        public string Vendor { get; set; } = null!;
        public string? SerialNumber { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
