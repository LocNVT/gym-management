using gym_management_server.Entities.Members;
using gym_management_server.Entities.Tenants;

namespace gym_management_server.Entities.FaceRecognition
{
    /// <summary>
    /// A biometric face template registered for a member. Mirrors
    /// Entities/Fingerprints/FingerprintTemplate.cs deliberately - see docs/ImprovementPlan.md mục 5
    /// (Hướng A: a parallel pipeline per modality, not a unified one, to keep this addition
    /// low-risk against the already-working fingerprint pipeline). Stores ONLY the encrypted,
    /// vendor-produced embedding/template bytes - never a raw face image.
    /// </summary>
    public class FaceTemplate : ITenantScoped
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid MemberId { get; set; }
        public Member Member { get; set; } = null!;

        /// <summary>Encrypted template/embedding payload (AES-GCM, via the same ITemplateProtector
        /// fingerprints use - it's already modality-agnostic). Decrypted only in memory during matching.</summary>
        public byte[] Template { get; set; } = Array.Empty<byte>();

        /// <summary>Template format vendor (e.g. a face SDK name, or "Mock"). Drives provider selection.</summary>
        public string Vendor { get; set; } = null!;

        /// <summary>Enrolment quality score (0-100), if reported by the provider/SDK.</summary>
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
