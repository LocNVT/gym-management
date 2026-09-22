using gym_management_server.DTOs.Export;
using gym_management_server.Infrastructure.Excel;

namespace gym_management_server.Services.ServicePackages
{
    public static class ServicePackageSheet
    {
        /// <summary>Columns a user may fill in. The headers, order and allowed values are
        /// fixed here so export and template cannot drift.</summary>
        public static SheetDefinition<ServicePackageRow> Import => new("Gói dịch vụ", new[]
        {
            new ExcelColumn<ServicePackageRow>("Tên gói", r => r.Name, isRequired: true, width: 28,
                parse: (r, v) => r.Name = v),
            new ExcelColumn<ServicePackageRow>("Mô tả", r => r.Description, width: 40,
                parse: (r, v) => r.Description = v),
            new ExcelColumn<ServicePackageRow>("Đơn giá", r => r.Price, format: "#,##0",
                parse: (r, v) => r.Price = CellParse.Money(v)),
            new ExcelColumn<ServicePackageRow>("Số ngày", r => r.DurationDays,
                parse: (r, v) => r.DurationDays = CellParse.Integer(v)),
            new ExcelColumn<ServicePackageRow>("Số lượt tối đa", r => r.MaxCheckins,
                parse: (r, v) => r.MaxCheckins = CellParse.Integer(v)),
            new ExcelColumn<ServicePackageRow>("Đang áp dụng", r => r.IsActive,
                parse: (r, v) => r.IsActive = CellParse.Boolean(v)),
        });

        public static SheetDefinition<ServicePackageRow> Export => Import.Plus(
            new ExcelColumn<ServicePackageRow>("Mã gói", r => r.Id, width: 38),
            new ExcelColumn<ServicePackageRow>("Ngày tạo", r => r.CreatedAt, format: "dd/MM/yyyy HH:mm"));
    }
}
