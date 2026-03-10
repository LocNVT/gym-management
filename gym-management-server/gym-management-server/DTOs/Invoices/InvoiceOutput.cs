namespace gym_management_server.DTOs.Invoices
{
    public class InvoiceOutput
    {
        public Guid Id { get; set; }
        public string InvoiceNumber { get; set; } = null!;
        public Guid? MemberId { get; set; }
        public DateTime InvoiceDate { get; set; }
        public decimal TotalAmount { get; set; }
        public byte Status { get; set; }
        public byte PaymentMethod { get; set; }
        public string? Notes { get; set; }
    }
}
