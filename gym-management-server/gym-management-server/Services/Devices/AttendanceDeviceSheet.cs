using gym_management_server.DTOs.Export;
using gym_management_server.Infrastructure.Excel;

namespace gym_management_server.Services.Devices
{
    public static class AttendanceDeviceSheet
    {
        public static SheetDefinition<AttendanceDeviceRow> Export => new("Thiết bị điểm danh", new[]
        {
            new ExcelColumn<AttendanceDeviceRow>("Mã thiết bị", r => r.Id, width: 38),
            new ExcelColumn<AttendanceDeviceRow>("Tên thiết bị", r => r.Name, width: 28),
            new ExcelColumn<AttendanceDeviceRow>("Vị trí", r => r.Location, width: 24),
            new ExcelColumn<AttendanceDeviceRow>("Hãng", r => r.Vendor),
            new ExcelColumn<AttendanceDeviceRow>("Số sê-ri", r => r.SerialNumber),
            new ExcelColumn<AttendanceDeviceRow>("Đang hoạt động", r => r.IsActive),
            new ExcelColumn<AttendanceDeviceRow>("Ngày tạo", r => r.CreatedAt, format: "dd/MM/yyyy HH:mm"),
        });
    }
}
