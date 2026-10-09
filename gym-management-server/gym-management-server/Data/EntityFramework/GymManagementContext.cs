using System.Text.Json;
using gym_management_server.Entities.Auditing;
using gym_management_server.Entities.CheckIns;
using gym_management_server.Entities.Devices;
using gym_management_server.Entities.Enums;
using gym_management_server.Entities.Expenses;
using gym_management_server.Entities.FaceRecognition;
using gym_management_server.Entities.Fingerprints;
using gym_management_server.Entities.InvoiceItems;
using gym_management_server.Entities.Invoices;
using gym_management_server.Entities.MemberDataServices;
using gym_management_server.Entities.Members;
using gym_management_server.Entities.OtpTokens;
using gym_management_server.Entities.ServicePackages;
using gym_management_server.Entities.Tenants;
using gym_management_server.Entities.Trainers;
using gym_management_server.Entities.Users;
using gym_management_server.Infrastructure.Auditing;
using gym_management_server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace gym_management_server.Data.EntityFramework
{
    public class GymManagementContext : DbContext
    {
        private readonly ICurrentUserAccessor _currentUser;
        private readonly ICurrentTenantAccessor _currentTenant;

        /// <summary>Entity types audited into <see cref="AuditLog"/>, each with its own properties
        /// excluded from the captured JSON (secrets, or anything not meaningful to review).
        /// "RowVersion" is excluded for every entity unconditionally - see <see cref="BuildAuditEntries"/>.</summary>
        private static readonly Dictionary<Type, HashSet<string>> AuditedEntities = new()
        {
            [typeof(User)] = new HashSet<string> { nameof(User.PasswordHash) },
            [typeof(Invoice)] = new HashSet<string>(),
            [typeof(InvoiceItem)] = new HashSet<string>(),
            [typeof(Expense)] = new HashSet<string>(),
            [typeof(CheckIn)] = new HashSet<string>(),
        };

        public GymManagementContext(DbContextOptions<GymManagementContext> options)
            : this(options, NullCurrentUserAccessor.Instance, new NullCurrentTenantAccessor()) { }

        public GymManagementContext(DbContextOptions<GymManagementContext> options, ICurrentUserAccessor currentUser)
            : this(options, currentUser, new NullCurrentTenantAccessor()) { }

        public GymManagementContext(
            DbContextOptions<GymManagementContext> options,
            ICurrentUserAccessor currentUser,
            ICurrentTenantAccessor currentTenant)
            : base(options)
        {
            _currentUser = currentUser;
            _currentTenant = currentTenant;
        }

        /// <summary>The accessor this context reads/stamps TenantId through - exposed so test code
        /// can hand a service under test the exact same instance (see FingerprintServiceTests).</summary>
        public ICurrentTenantAccessor CurrentTenant => _currentTenant;


        public DbSet<Tenant> Tenants => Set<Tenant>();
        public DbSet<Member> Members => Set<Member>();
        public DbSet<ServicePackage> ServicePackages => Set<ServicePackage>();
        public DbSet<MemberDataService> MemberDataServices => Set<MemberDataService>();
        public DbSet<CheckIn> CheckIns => Set<CheckIn>();
        public DbSet<Invoice> Invoices => Set<Invoice>();
        public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
        public DbSet<Expense> Expenses => Set<Expense>();
        public DbSet<Trainer> Trainers => Set<Trainer>();
        public DbSet<User> Users => Set<User>();
        public DbSet<OtpToken> OtpTokens => Set<OtpToken>();
        public DbSet<FingerprintTemplate> FingerprintTemplates => Set<FingerprintTemplate>();
        public DbSet<FaceTemplate> FaceTemplates => Set<FaceTemplate>();
        public DbSet<AttendanceDevice> AttendanceDevices => Set<AttendanceDevice>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();


        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);


            b.Entity<Tenant>(e =>
            {
                e.Property(x => x.Name).HasMaxLength(150).IsRequired();
                e.Property(x => x.Address).HasMaxLength(250);
                e.Property(x => x.Phone).HasMaxLength(30);
            });

            b.Entity<Member>(e =>
            {
                // Unique per tenant, not globally: two different gyms can each have a member with
                // the same phone number.
                e.HasIndex(x => new { x.TenantId, x.PhoneNumber }).IsUnique();
                e.Property(x => x.FullName).HasMaxLength(150).IsRequired();
                e.Property(x => x.Email).HasMaxLength(150);
                e.Property(x => x.RowVersion).IsRowVersion();
                ConfigureTenantScoped(e);
            });


            b.Entity<ServicePackage>(e =>
            {
                e.Property(x => x.Price).HasColumnType("decimal(18,2)");
                e.Property(x => x.Name).HasMaxLength(150).IsRequired();
                e.Property(x => x.RowVersion).IsRowVersion();
                ConfigureTenantScoped(e);
            });


            b.Entity<MemberDataService>(e =>
            {
                e.Property(x => x.PriceAtPurchase).HasColumnType("decimal(18,2)");
                e.HasOne(x => x.Member).WithMany(m => m.MemberServices).HasForeignKey(x => x.MemberId);
                e.HasOne(x => x.ServicePackage).WithMany(s => s.MemberDataServices).HasForeignKey(x => x.ServicePackageId);
                e.HasIndex(x => new { x.MemberId, x.Status });
                ConfigureTenantScoped(e);
            });


            b.Entity<CheckIn>(e =>
            {
                e.HasOne(x => x.Member).WithMany(m => m.CheckIns).HasForeignKey(x => x.MemberId);
                e.HasOne(x => x.Device).WithMany().HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.SetNull);
                e.HasIndex(x => new { x.MemberId, x.CheckInTime });
                // Fast lookup of a member's currently-open attendance session.
                e.HasIndex(x => new { x.MemberId, x.CheckOutTime });
                // At most one open session per member, enforced at the DB level so two near-simultaneous
                // scans/requests can never both insert a check-in for the same member (see
                // FingerprintService.VerifyAsync, which falls back to a check-out when this is violated).
                e.HasIndex(x => x.MemberId)
                    .IsUnique()
                    .HasDatabaseName("IX_CheckIns_MemberId_ActiveSession")
                    .HasFilter("[CheckOutTime] IS NULL");
                e.Property(x => x.RowVersion).IsRowVersion();
                ConfigureTenantScoped(e);
            });

            b.Entity<FingerprintTemplate>(e =>
            {
                e.HasOne(x => x.Member).WithMany(m => m.FingerprintTemplates).HasForeignKey(x => x.MemberId);
                e.Property(x => x.Vendor).HasMaxLength(50).IsRequired();
                e.Property(x => x.RowVersion).IsRowVersion();
                e.HasIndex(x => x.MemberId);
                // One active template per finger per member (filtered so soft-deleted rows don't collide).
                e.HasIndex(x => new { x.MemberId, x.FingerPosition })
                    .IsUnique()
                    .HasFilter("[IsDeleted] = 0");
                ConfigureTenantScoped(e);
            });

            b.Entity<AttendanceDevice>(e =>
            {
                e.Property(x => x.Name).HasMaxLength(150).IsRequired();
                e.Property(x => x.Vendor).HasMaxLength(50).IsRequired();
                e.Property(x => x.Location).HasMaxLength(200);
                e.Property(x => x.SerialNumber).HasMaxLength(100);
                e.Property(x => x.RowVersion).IsRowVersion();
                // A serial number is physically unique regardless of tenant, unlike the other
                // indexes here - deliberately NOT scoped to TenantId.
                e.HasIndex(x => x.SerialNumber).IsUnique().HasFilter("[SerialNumber] IS NOT NULL");
                ConfigureTenantScoped(e);
            });

            b.Entity<FaceTemplate>(e =>
            {
                e.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId);
                e.Property(x => x.Vendor).HasMaxLength(50).IsRequired();
                e.Property(x => x.RowVersion).IsRowVersion();
                // One active face template per member (unlike fingerprints, which allow several -
                // one per finger - a member enrols a single face profile). Filtered so soft-deleted
                // rows don't collide with a re-enrolment.
                e.HasIndex(x => x.MemberId)
                    .IsUnique()
                    .HasDatabaseName("IX_FaceTemplates_MemberId_Active")
                    .HasFilter("[IsDeleted] = 0");
                ConfigureTenantScoped(e);
            });


            b.Entity<Invoice>(e =>
            {
                e.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
                // Unique per tenant: two different gyms can each number their invoices from 1.
                e.HasIndex(x => new { x.TenantId, x.InvoiceNumber }).IsUnique();
                e.HasOne(x => x.Member).WithMany(m => m.Invoices);
                e.Property(x => x.RowVersion).IsRowVersion();
                ConfigureTenantScoped(e);
            });

            b.Entity<InvoiceItem>(e =>
            {
                e.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
                e.Property(x => x.RowVersion).IsRowVersion();
                ConfigureTenantScoped(e);
            });


            b.Entity<Expense>(e =>
            {
                e.Property(x => x.Amount).HasColumnType("decimal(18,2)");
                e.Property(x => x.RowVersion).IsRowVersion();
                ConfigureTenantScoped(e);
            });


            b.Entity<Trainer>(e =>
            {
                e.Property(x => x.HourlyRate).HasColumnType("decimal(18,2)");
                e.Property(x => x.FullName).HasMaxLength(150).IsRequired();
                e.Property(x => x.Email).HasMaxLength(150);
                e.Property(x => x.RowVersion).IsRowVersion();
                ConfigureTenantScoped(e);
            });


            b.Entity<User>(e =>
            {
                // Deliberately GLOBAL, not per-tenant: a username/email must resolve to exactly one
                // tenant at login time, since login happens before any tenant is known. The
                // trade-off is that two different gyms can't both have a staff member named
                // "admin" - see docs/ImprovementPlan.md mục 1.
                e.HasIndex(x => x.Username).IsUnique();
                e.HasIndex(x => x.Email).IsUnique();
                e.Property(x => x.Username).HasMaxLength(100).IsRequired();
                e.Property(x => x.Email).HasMaxLength(150).IsRequired();
                e.Property(x => x.FullName).HasMaxLength(150).IsRequired();
                e.Property(x => x.RowVersion).IsRowVersion();
                ConfigureTenantScoped(e);
            });

            b.Entity<OtpToken>(e =>
            {
                e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
                e.Property(x => x.OtpCode).HasMaxLength(6).IsRequired();
                e.HasIndex(x => new { x.UserId, x.OtpCode, x.Purpose });
            });

            b.Entity<AuditLog>(e =>
            {
                e.Property(x => x.EntityName).HasMaxLength(100).IsRequired();
                e.Property(x => x.EntityId).HasMaxLength(100).IsRequired();
                e.Property(x => x.ActorUsername).HasMaxLength(100);
                e.HasIndex(x => new { x.EntityName, x.EntityId });
                e.HasIndex(x => x.Timestamp);
                ConfigureTenantScoped(e);
            });

            // Seed a default tenant for the seeded admin below, and for docs/ImprovementPlan.md
            // mục 1's migration (every pre-existing row is backfilled into this tenant).
            var defaultTenantId = Guid.Parse("00000000-0000-0000-0000-0000000000AA");
            b.Entity<Tenant>().HasData(new Tenant
            {
                Id = defaultTenantId,
                Name = "Chi nhánh mặc định",
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });

            // Seed admin user
            b.Entity<User>().HasData(new User
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                TenantId = defaultTenantId,
                Username = "admin",
                Email = "admin@gym.com",
                PasswordHash = "$2a$11$7MFWqEGHZ8Gg54pQQ3pYRuBtk6Ug9HqpgO.PoO84GyDqOnYJpp10e", // Matkhau@2
                FullName = "Administrator",
                Role = 1, // Admin
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
        }

        /// <summary>Configures the TenantId index and global query filter shared by every
        /// ITenantScoped entity - see docs/ImprovementPlan.md mục 1.</summary>
        private void ConfigureTenantScoped<TEntity>(EntityTypeBuilder<TEntity> builder)
            where TEntity : class, ITenantScoped
        {
            builder.HasIndex(x => x.TenantId);
            builder.HasQueryFilter(x => x.TenantId == _currentTenant.TenantId);
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            StampTenantId();
            BuildAuditEntries();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            StampTenantId();
            BuildAuditEntries();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        /// <summary>
        /// Stamps the ambient tenant (see ICurrentTenantAccessor) onto every newly-added
        /// ITenantScoped entity that doesn't already have one explicitly set - which is most of
        /// them, since this is what lets services create Member/Invoice/CheckIn/etc. rows without
        /// ever touching TenantId themselves. An entity that set its own TenantId already (the
        /// first User of a brand-new Tenant during registration, or a fingerprint-device-scoped
        /// CheckIn - see FingerprintService.VerifyAsync) is left alone.
        /// </summary>
        private void StampTenantId()
        {
            var tenantId = _currentTenant.TenantId;
            if (tenantId is null) return;

            foreach (var entry in ChangeTracker.Entries<ITenantScoped>())
            {
                if (entry.State == EntityState.Added && entry.Entity.TenantId == Guid.Empty)
                    entry.Entity.TenantId = tenantId.Value;
            }
        }

        /// <summary>
        /// Resets every entity this context is tracking (other than Unchanged ones) back to
        /// Detached. Call after a failed SaveChanges/SaveChangesAsync on a context you intend to
        /// keep using: a failed save leaves its entities (and, since mục 6, any AuditLog rows
        /// BuildAuditEntries staged for that same save) still tracked as Added/Modified/Deleted,
        /// and the next unrelated SaveChanges on this context would silently retry and persist them.
        /// </summary>
        public void DiscardPendingChanges()
        {
            foreach (var entry in ChangeTracker.Entries().Where(e => e.State != EntityState.Unchanged).ToList())
                entry.State = EntityState.Detached;
        }

        /// <summary>
        /// Captures one AuditLog row per tracked Added/Modified/Deleted entity whose type is in
        /// <see cref="AuditedEntities"/>, and stages them into this same change set so they are
        /// persisted atomically with the change they describe. Must run BEFORE the base
        /// SaveChanges call: EF resets OriginalValues to match CurrentValues once a save succeeds,
        /// so the "before" snapshot is only available right up until that point.
        /// </summary>
        private void BuildAuditEntries()
        {
            ChangeTracker.DetectChanges();

            var entries = ChangeTracker.Entries()
                .Where(e => AuditedEntities.ContainsKey(e.Entity.GetType())
                    && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                .ToList();

            if (entries.Count == 0) return;

            var userId = _currentUser.UserId;
            var username = _currentUser.Username;
            var now = DateTime.UtcNow;

            foreach (var entry in entries)
            {
                var excluded = AuditedEntities[entry.Entity.GetType()];
                var action = entry.State switch
                {
                    EntityState.Added => AuditAction.Create,
                    EntityState.Deleted => AuditAction.Delete,
                    _ => AuditAction.Update
                };

                Set<AuditLog>().Add(new AuditLog
                {
                    Id = Guid.NewGuid(),
                    // Set explicitly, not left to StampTenantId: that pass already ran earlier in
                    // this same SaveChanges call, before this row existed.
                    TenantId = _currentTenant.TenantId ?? Guid.Empty,
                    EntityName = entry.Entity.GetType().Name,
                    EntityId = DescribePrimaryKey(entry),
                    Action = action,
                    ActorUserId = userId,
                    ActorUsername = username,
                    Timestamp = now,
                    OldValuesJson = action == AuditAction.Create ? null : SerializeValues(entry, excluded, useOriginal: true),
                    NewValuesJson = action == AuditAction.Delete ? null : SerializeValues(entry, excluded, useOriginal: false),
                });
            }
        }

        private static string DescribePrimaryKey(EntityEntry entry)
        {
            var keyProperties = entry.Metadata.FindPrimaryKey()?.Properties;
            if (keyProperties == null || keyProperties.Count == 0) return "unknown";
            return string.Join(",", keyProperties.Select(p => entry.Property(p.Name).CurrentValue));
        }

        private static string SerializeValues(EntityEntry entry, HashSet<string> excluded, bool useOriginal)
        {
            var values = entry.Properties
                .Where(p => p.Metadata.Name != nameof(AuditLog.Id) // never relevant on the entry itself
                    && p.Metadata.Name != "RowVersion"
                    && !excluded.Contains(p.Metadata.Name))
                .ToDictionary(p => p.Metadata.Name, p => useOriginal ? p.OriginalValue : p.CurrentValue);
            return JsonSerializer.Serialize(values);
        }
    }
}

