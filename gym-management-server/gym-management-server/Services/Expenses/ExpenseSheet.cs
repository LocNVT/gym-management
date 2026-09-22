using gym_management_server.DTOs.Export;
using gym_management_server.Infrastructure.Enums;
using gym_management_server.Infrastructure.Excel;

namespace gym_management_server.Services.Expenses
{
    public static class ExpenseSheet
    {
        public static SheetDefinition<ExpenseRow> Export => new("Chi phí", new[]
        {
            new ExcelColumn<ExpenseRow>("Mã chi phí", r => r.Id, width: 38),
            new ExcelColumn<ExpenseRow>("Ngày chi", r => r.ExpenseDate, format: "dd/MM/yyyy"),
            new ExcelColumn<ExpenseRow>("Hạng mục", r => r.Category, width: 22),
            new ExcelColumn<ExpenseRow>("Diễn giải", r => r.Description, width: 34),
            new ExcelColumn<ExpenseRow>("Số tiền", r => r.Amount, format: "#,##0"),
            new ExcelColumn<ExpenseRow>("Hình thức", r => r.PaymentMethod.ToLabel()),
            new ExcelColumn<ExpenseRow>("Ghi chú", r => r.Notes, width: 34),
        });
    }
}
