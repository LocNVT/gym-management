using gym_management_server.Entities.CheckIns;
using gym_management_server.Entities.Expenses;
using gym_management_server.Entities.InvoiceItems;
using gym_management_server.Entities.Invoices;
using gym_management_server.Entities.MemberDataServices;
using gym_management_server.Entities.Members;
using gym_management_server.Entities.OtpTokens;
using gym_management_server.Entities.ServicePackages;
using gym_management_server.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace gym_management_server.Data.EntityFramework
{
    public class GymManagementContext : DbContext
    {
        public GymManagementContext(DbContextOptions<GymManagementContext> options) : base(options) { }


        public DbSet<Member> Members => Set<Member>();
        public DbSet<ServicePackage> ServicePackages => Set<ServicePackage>();
        public DbSet<MemberDataService> MemberDataServices => Set<MemberDataService>();
        public DbSet<CheckIn> CheckIns => Set<CheckIn>();
        public DbSet<Invoice> Invoices => Set<Invoice>();
        public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
        public DbSet<Expense> Expenses => Set<Expense>();
        public DbSet<User> Users => Set<User>();
        public DbSet<OtpToken> OtpTokens => Set<OtpToken>();


        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);


            b.Entity<Member>(e =>
            {
                e.HasIndex(x => x.PhoneNumber).IsUnique();
                e.Property(x => x.FullName).HasMaxLength(150).IsRequired();
                e.Property(x => x.Email).HasMaxLength(150);
            });


            b.Entity<ServicePackage>(e =>
            {
                e.Property(x => x.Price).HasColumnType("decimal(18,2)");
                e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            });


            b.Entity<MemberDataService>(e =>
            {
                e.Property(x => x.PriceAtPurchase).HasColumnType("decimal(18,2)");
                e.HasOne(x => x.Member).WithMany(m => m.MemberServices).HasForeignKey(x => x.MemberId);
                e.HasOne(x => x.ServicePackage).WithMany(s => s.MemberDataServices).HasForeignKey(x => x.ServicePackageId);
                e.HasIndex(x => new { x.MemberId, x.Status });
            });


            b.Entity<CheckIn>(e =>
            {
                e.HasOne(x => x.Member).WithMany(m => m.CheckIns).HasForeignKey(x => x.MemberId);
                e.HasIndex(x => new { x.MemberId, x.CheckInTime });
            });


            b.Entity<Invoice>(e =>
            {
                e.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
                e.HasIndex(x => x.InvoiceNumber).IsUnique();
                e.HasOne(x => x.Member).WithMany(m => m.Invoices);
            });

            b.Entity<InvoiceItem>(e =>
            {
                e.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            });


            b.Entity<Expense>(e =>
            {
                e.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            });


            b.Entity<User>(e =>
            {
                e.HasIndex(x => x.Username).IsUnique();
                e.HasIndex(x => x.Email).IsUnique();
                e.Property(x => x.Username).HasMaxLength(100).IsRequired();
                e.Property(x => x.Email).HasMaxLength(150).IsRequired();
                e.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            });

            b.Entity<OtpToken>(e =>
            {
                e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
                e.Property(x => x.OtpCode).HasMaxLength(6).IsRequired();
                e.HasIndex(x => new { x.UserId, x.OtpCode, x.Purpose });
            });

            // Seed admin user
            b.Entity<User>().HasData(new User
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Username = "admin",
                Email = "admin@gym.com",
                PasswordHash = "$2a$11$7MFWqEGHZ8Gg54pQQ3pYRuBtk6Ug9HqpgO.PoO84GyDqOnYJpp10e", // Matkhau@2
                FullName = "Administrator",
                Role = 1, // Admin
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
        }
    }
}

