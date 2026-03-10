namespace gym_management_server.DTOs.Invoices
{
    public class InvoiceInput
    {
        public string InvoiceNumber { get; set; } = null!;
        public Guid? MemberId { get; set; }
        public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
        public decimal TotalAmount { get; set; }
        public byte Status { get; set; } = 1;
        public byte PaymentMethod { get; set; } = 0;
        public string? Notes { get; set; }
    }
}
