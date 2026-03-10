namespace gym_management_server.DTOs.InvoiceItems
{
    public class InvoiceItemInput
    {
        public Guid InvoiceId { get; set; }
        public string Description { get; set; } = null!;
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public int? ServicePackageId { get; set; }
    }
}
