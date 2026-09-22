using gym_management_server.Entities.Enums;

namespace gym_management_server.Entities.Expenses
{
    public class Expense
    {
        public Guid Id { get; set; }
        public DateTime ExpenseDate { get; set; }
        public string Category { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
        public string? Notes { get; set; }
    }
}
