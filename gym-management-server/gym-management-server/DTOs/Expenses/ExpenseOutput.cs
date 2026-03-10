namespace gym_management_server.DTOs.Expenses
{
    public class ExpenseOutput
    {
        public Guid Id { get; set; }
        public DateTime ExpenseDate { get; set; }
        public string Category { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public byte PaymentMethod { get; set; }
        public string? Notes { get; set; }
    }
}
