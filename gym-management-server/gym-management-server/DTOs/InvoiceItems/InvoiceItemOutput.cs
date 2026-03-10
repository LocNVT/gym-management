namespace gym_management_server.DTOs.InvoiceItems
{
    public class InvoiceItemOutput
    {
        public Guid Id { get; set; }
        public Guid InvoiceId { get; set; }
        public string Description { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public int? ServicePackageId { get; set; }
    }
}
