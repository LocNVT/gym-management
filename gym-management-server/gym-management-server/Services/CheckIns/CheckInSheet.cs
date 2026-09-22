using gym_management_server.DTOs.Export;
using gym_management_server.Infrastructure.Enums;
using gym_management_server.Infrastructure.Excel;

namespace gym_management_server.Services.CheckIns
{
    public static class CheckInSheet
    {
        public static SheetDefinition<AttendanceRow> Export => new("Lịch sử điểm danh", new[]
        {
            new ExcelColumn<AttendanceRow>("Mã phiên", r => r.Id, width: 38),
            new ExcelColumn<AttendanceRow>("Hội viên", r => r.MemberName, width: 26),
            new ExcelColumn<AttendanceRow>("Số điện thoại", r => r.MemberPhone),
            new ExcelColumn<AttendanceRow>("Giờ vào", r => r.CheckInTime, format: "dd/MM/yyyy HH:mm"),
            new ExcelColumn<AttendanceRow>("Giờ ra", r => r.CheckOutTime, format: "dd/MM/yyyy HH:mm"),
            new ExcelColumn<AttendanceRow>("Số phút", r => r.MinutesInside),
            new ExcelColumn<AttendanceRow>("Hình thức", r => r.Method.ToLabel()),
            new ExcelColumn<AttendanceRow>("Thiết bị", r => r.DeviceName, width: 24),
            new ExcelColumn<AttendanceRow>("Ghi chú", r => r.Notes, width: 34),
        });
    }
}
