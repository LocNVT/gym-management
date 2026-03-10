using gym_management_server.Entities.Invoices;

namespace gym_management_server.Entities.InvoiceItems
{
    public class InvoiceItem
    {
        public Guid Id { get; set; }
        public Guid InvoiceId { get; set; }
        public Invoice Invoice { get; set; } = null!;
        public string Description { get; set; } = null!;
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public int? ServicePackageId { get; set; }
    }
}
