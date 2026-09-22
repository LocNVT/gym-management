using gym_management_server.Entities.Enums;

namespace gym_management_server.DTOs.Invoices
{
    public class InvoiceInput
    {
        public string InvoiceNumber { get; set; } = null!;
        public Guid? MemberId { get; set; }
        public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
        public decimal TotalAmount { get; set; }
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Pending;
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
        public string? Notes { get; set; }
    }
}
