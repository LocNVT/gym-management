using gym_management_server.Entities.Members;

namespace gym_management_server.Entities.Fingerprints
{
    /// <summary>
    /// A biometric fingerprint template registered for a member.
    /// Stores ONLY the encrypted, vendor-produced template bytes — never a raw fingerprint image.
    /// </summary>
    public class FingerprintTemplate
    {
        public Guid Id { get; set; }

        public Guid MemberId { get; set; }
        public Member Member { get; set; } = null!;

        /// <summary>Which finger this template represents (0-9). Lets a member enrol several fingers.</summary>
        public byte FingerPosition { get; set; }

        /// <summary>Encrypted template payload (AES-GCM). Decrypted only in memory during matching.</summary>
        public byte[] Template { get; set; } = Array.Empty<byte>();

        /// <summary>Template format vendor (e.g. ZKTeco, Suprema, DigitalPersona, Mock). Drives provider selection.</summary>
        public string Vendor { get; set; } = null!;

        /// <summary>Enrolment quality score (0-100), if reported by the device.</summary>
        public byte Quality { get; set; }

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
