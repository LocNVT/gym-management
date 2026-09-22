using gym_management_server.Entities.Enums;

namespace gym_management_server.DTOs.Export
{
    public class MemberRow
    {
        public string FullName { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public string? Email { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public Gender? Gender { get; set; }
        public string? Address { get; set; }
        public string? EmergencyName { get; set; }
        public string? EmergencyPhone { get; set; }
        public MemberStatus Status { get; set; } = MemberStatus.Active;
        public string? Notes { get; set; }

        // Read-only on import.
        public Guid Id { get; set; }
        public DateTime RegistrationDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ServicePackageRow
    {
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int DurationDays { get; set; }
        public int? MaxCheckins { get; set; }

        // Matches ServicePackage.IsActive's and ServicePackageInput.IsActive's own default.
        // The "Đang áp dụng" column is optional on import, so a blank cell must produce the
        // same package a blank field produces everywhere else in the app -- active -- not a
        // silently disabled one.
        public bool IsActive { get; set; } = true;

        // Read-only on import.
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class TrainerRow
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = "";
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Specialty { get; set; }
        public decimal HourlyRate { get; set; }
        public TrainerStatus Status { get; set; }
        public string? Notes { get; set; }
    }

    public class AttendanceDeviceRow
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string? Location { get; set; }
        public string Vendor { get; set; } = "";
        public string? SerialNumber { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ExpenseRow
    {
        public Guid Id { get; set; }
        public DateTime ExpenseDate { get; set; }
        public string Category { get; set; } = "";
        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string? Notes { get; set; }
    }

    public class InvoiceRow
    {
        public Guid Id { get; set; }
        public string InvoiceNumber { get; set; } = "";
        public string? MemberName { get; set; }
        public string? MemberPhone { get; set; }
        public DateTime InvoiceDate { get; set; }
        public decimal TotalAmount { get; set; }
        public InvoiceStatus Status { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string? Notes { get; set; }
    }

    public class InvoiceItemRow
    {
        public string InvoiceNumber { get; set; } = "";
        public string Description { get; set; } = "";
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }

    public class SubscriptionRow
    {
        public Guid Id { get; set; }
        public string MemberName { get; set; } = "";
        public string MemberPhone { get; set; } = "";
        public string PackageName { get; set; } = "";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal PriceAtPurchase { get; set; }
        public int? RemainingCheckins { get; set; }
        public SubscriptionStatus Status { get; set; }
    }

    public class AttendanceRow
    {
        public Guid Id { get; set; }
        public string MemberName { get; set; } = "";
        public string MemberPhone { get; set; } = "";
        public DateTime CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public int? MinutesInside { get; set; }
        public CheckInMethod Method { get; set; }
        public string? DeviceName { get; set; }
        public string? Notes { get; set; }
    }
}
