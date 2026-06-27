namespace gym_management_server.DTOs.Devices
{
    public class AttendanceDeviceInput
    {
        public string Name { get; set; } = null!;
        public string? Location { get; set; }
        public string Vendor { get; set; } = "Mock";
        public string? SerialNumber { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
