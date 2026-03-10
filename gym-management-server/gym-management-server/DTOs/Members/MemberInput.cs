using gym_management_server.Entities.MemberDataServices;

namespace gym_management_server.DTOs.Members
{
    public class MemberInput
    {
        //public Guid Id { get; set; }
        public string FullName { get; set; } = null!;
        public DateTime? DateOfBirth { get; set; }
        public byte? Gender { get; set; }
        public string PhoneNumber { get; set; } = null!;
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? EmergencyName { get; set; }
        public string? EmergencyPhone { get; set; }
        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
        public byte Status { get; set; } = 1;
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;

        public string? AvatarUrl { get; set; }


        //public List<MemberDataService> MemberServices { get; set; } = new();
        //public List<CheckIn> CheckIns { get; set; } = new();
        //public List<Invoice> Invoices { get; set; } = new();
    }
}
