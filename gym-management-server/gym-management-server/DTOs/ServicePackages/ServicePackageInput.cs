namespace gym_management_server.DTOs.ServicePackages
{
    public class ServicePackageInput
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int DurationDays { get; set; }
        public int? MaxCheckins { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
