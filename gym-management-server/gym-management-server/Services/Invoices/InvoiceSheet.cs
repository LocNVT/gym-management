using gym_management_server.DTOs.Export;
using gym_management_server.Infrastructure.Enums;
using gym_management_server.Infrastructure.Excel;

namespace gym_management_server.Services.Invoices
{
    public static class InvoiceSheet
    {
        public static SheetDefinition<InvoiceRow> Export => new("Hóa đơn", new[]
        {
            new ExcelColumn<InvoiceRow>("Số hóa đơn", r => r.InvoiceNumber),
            new ExcelColumn<InvoiceRow>("Hội viên", r => r.MemberName, width: 28),
            new ExcelColumn<InvoiceRow>("Số điện thoại", r => r.MemberPhone),
            new ExcelColumn<InvoiceRow>("Ngày hóa đơn", r => r.InvoiceDate, format: "dd/MM/yyyy"),
            new ExcelColumn<InvoiceRow>("Tổng tiền", r => r.TotalAmount, format: "#,##0"),
            new ExcelColumn<InvoiceRow>("Trạng thái", r => r.Status.ToLabel()),
            new ExcelColumn<InvoiceRow>("Hình thức", r => r.PaymentMethod.ToLabel()),
            new ExcelColumn<InvoiceRow>("Ghi chú", r => r.Notes, width: 30),
            new ExcelColumn<InvoiceRow>("Mã hóa đơn", r => r.Id, width: 38),
        });

        public static SheetDefinition<InvoiceItemRow> Items => new("Chi tiết hóa đơn", new[]
        {
            new ExcelColumn<InvoiceItemRow>("Số hóa đơn", r => r.InvoiceNumber),
            new ExcelColumn<InvoiceItemRow>("Diễn giải", r => r.Description, width: 34),
            new ExcelColumn<InvoiceItemRow>("Số lượng", r => r.Quantity),
            new ExcelColumn<InvoiceItemRow>("Đơn giá", r => r.UnitPrice, format: "#,##0"),
            new ExcelColumn<InvoiceItemRow>("Thành tiền", r => r.LineTotal, format: "#,##0"),
        });
    }
}
