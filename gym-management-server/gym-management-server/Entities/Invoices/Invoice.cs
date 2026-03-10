using gym_management_server.Entities.InvoiceItems;
using gym_management_server.Entities.Members;

namespace gym_management_server.Entities.Invoices
{
    public class Invoice
    {
        public Guid Id { get; set; }
        public string InvoiceNumber { get; set; } = null!;
        public Guid? MemberId { get; set; }
        public Member? Member { get; set; }
        public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
        public decimal TotalAmount { get; set; }
        public byte Status { get; set; } = 1;
        public byte PaymentMethod { get; set; } = 0;
        public string? Notes { get; set; }


        public List<InvoiceItem> Items { get; set; } = new();
    }
}
