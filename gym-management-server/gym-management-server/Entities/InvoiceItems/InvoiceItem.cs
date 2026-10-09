using gym_management_server.Entities.Invoices;
using gym_management_server.Entities.Tenants;

namespace gym_management_server.Entities.InvoiceItems
{
    public class InvoiceItem : ITenantScoped
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid InvoiceId { get; set; }
        public Invoice Invoice { get; set; } = null!;
        public string Description { get; set; } = null!;
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public int? ServicePackageId { get; set; }

        public byte[]? RowVersion { get; set; }
    }
}
