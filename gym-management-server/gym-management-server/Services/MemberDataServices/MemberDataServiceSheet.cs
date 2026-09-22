using gym_management_server.DTOs.Export;
using gym_management_server.Infrastructure.Enums;
using gym_management_server.Infrastructure.Excel;

namespace gym_management_server.Services.MemberDataServices
{
    public static class MemberDataServiceSheet
    {
        public static SheetDefinition<SubscriptionRow> Export => new("Đăng ký gói", new[]
        {
            new ExcelColumn<SubscriptionRow>("Mã đăng ký", r => r.Id, width: 38),
            new ExcelColumn<SubscriptionRow>("Hội viên", r => r.MemberName, width: 26),
            new ExcelColumn<SubscriptionRow>("Số điện thoại", r => r.MemberPhone),
            new ExcelColumn<SubscriptionRow>("Gói dịch vụ", r => r.PackageName, width: 24),
            new ExcelColumn<SubscriptionRow>("Ngày bắt đầu", r => r.StartDate, format: "dd/MM/yyyy"),
            new ExcelColumn<SubscriptionRow>("Ngày kết thúc", r => r.EndDate, format: "dd/MM/yyyy"),
            new ExcelColumn<SubscriptionRow>("Giá lúc mua", r => r.PriceAtPurchase, format: "#,##0"),
            new ExcelColumn<SubscriptionRow>("Lượt còn lại", r => r.RemainingCheckins),
            new ExcelColumn<SubscriptionRow>("Trạng thái", r => r.Status.ToLabel()),
        });
    }
}
