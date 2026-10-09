using gym_management_server.Entities.Tenants;

namespace gym_management_server.Entities.Devices
{
    /// <summary>
    /// A physical fingerprint scanner registered with the system.
    /// The <see cref="Vendor"/> drives which IFingerprintProvider handles its templates,
    /// keeping business logic decoupled from any specific hardware vendor.
    /// </summary>
    public class AttendanceDevice : ITenantScoped
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public string Name { get; set; } = null!;
        public string? Location { get; set; }

        /// <summary>Vendor key resolved by the provider factory (e.g. ZKTeco, Suprema, DigitalPersona, Mock).</summary>
        public string Vendor { get; set; } = null!;

        public string? SerialNumber { get; set; }

        public bool IsActive { get; set; } = true;

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public Guid? CreatedBy { get; set; }

        // Soft delete
        public bool IsDeleted { get; set; } = false;

        // Optimistic locking
        public byte[]? RowVersion { get; set; }
    }
}
