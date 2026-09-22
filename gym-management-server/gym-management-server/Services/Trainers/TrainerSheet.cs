using gym_management_server.DTOs.Export;
using gym_management_server.Infrastructure.Enums;
using gym_management_server.Infrastructure.Excel;

namespace gym_management_server.Services.Trainers
{
    public static class TrainerSheet
    {
        public static SheetDefinition<TrainerRow> Export => new("Huấn luyện viên", new[]
        {
            new ExcelColumn<TrainerRow>("Mã HLV", r => r.Id, width: 38),
            new ExcelColumn<TrainerRow>("Họ và tên", r => r.FullName, width: 28),
            new ExcelColumn<TrainerRow>("Số điện thoại", r => r.PhoneNumber),
            new ExcelColumn<TrainerRow>("Email", r => r.Email, width: 26),
            new ExcelColumn<TrainerRow>("Chuyên môn", r => r.Specialty, width: 24),
            new ExcelColumn<TrainerRow>("Giá theo giờ", r => r.HourlyRate, format: "#,##0"),
            new ExcelColumn<TrainerRow>("Trạng thái", r => r.Status.ToLabel()),
            new ExcelColumn<TrainerRow>("Ghi chú", r => r.Notes, width: 34),
        });
    }
}
