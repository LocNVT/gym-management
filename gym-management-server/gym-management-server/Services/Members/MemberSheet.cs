using gym_management_server.DTOs.Export;
using gym_management_server.Entities.Enums;
using gym_management_server.Infrastructure.Enums;
using gym_management_server.Infrastructure.Excel;

namespace gym_management_server.Services.Members
{
    public static class MemberSheet
    {
        private static IReadOnlyList<string> Labels<TEnum>() where TEnum : struct, Enum =>
            EnumLabel.Describe<TEnum>().Select(v => v.Label).ToList();

        /// <summary>Columns a user may fill in. Parse is filled in by plan 2; the headers,
        /// order and allowed values are fixed here so export and template cannot drift.</summary>
        public static SheetDefinition<MemberRow> Import => new("Thành viên", new[]
        {
            new ExcelColumn<MemberRow>("Họ và tên", r => r.FullName, isRequired: true, width: 28,
                parse: (r, v) => r.FullName = CellParse.Text(v, 150, "Họ và tên")),
            new ExcelColumn<MemberRow>("Số điện thoại", r => r.PhoneNumber, isRequired: true,
                parse: (r, v) => r.PhoneNumber = CellParse.Phone(v)),
            new ExcelColumn<MemberRow>("Email", r => r.Email, width: 26,
                parse: (r, v) => r.Email = CellParse.Email(CellParse.Text(v, 150, "Email"))),
            new ExcelColumn<MemberRow>("Ngày sinh", r => r.DateOfBirth, format: "dd/MM/yyyy",
                parse: (r, v) => r.DateOfBirth = CellParse.Date(v)),
            new ExcelColumn<MemberRow>("Giới tính", r => r.Gender.ToLabel(), allowedValues: Labels<Gender>(),
                parse: (r, v) => r.Gender = CellParse.Enum<Gender>(v)),
            new ExcelColumn<MemberRow>("Địa chỉ", r => r.Address, width: 34,
                parse: (r, v) => r.Address = v),
            new ExcelColumn<MemberRow>("Người liên hệ khẩn cấp", r => r.EmergencyName, width: 24,
                parse: (r, v) => r.EmergencyName = v),
            new ExcelColumn<MemberRow>("SĐT khẩn cấp", r => r.EmergencyPhone,
                parse: (r, v) => r.EmergencyPhone = CellParse.Phone(v)),
            new ExcelColumn<MemberRow>("Trạng thái", r => r.Status.ToLabel(), allowedValues: Labels<MemberStatus>(),
                parse: (r, v) => r.Status = CellParse.Enum<MemberStatus>(v)),
            new ExcelColumn<MemberRow>("Ghi chú", r => r.Notes, width: 34,
                parse: (r, v) => r.Notes = v),
        });

        public static SheetDefinition<MemberRow> Export => Import.Plus(
            new ExcelColumn<MemberRow>("Mã hội viên", r => r.Id, width: 38),
            new ExcelColumn<MemberRow>("Ngày đăng ký", r => r.RegistrationDate, format: "dd/MM/yyyy"),
            new ExcelColumn<MemberRow>("Ngày tạo", r => r.CreatedAt, format: "dd/MM/yyyy HH:mm"));
    }
}
