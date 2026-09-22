using gym_management_server.DTOs.Export;
using gym_management_server.Entities.Enums;
using gym_management_server.Services.Invoices;
using gym_management_server.Services.Members;
using Xunit;

namespace gym_management_server.Tests.Unit
{
    public class SheetDefinitionTests
    {
        [Fact]
        public void Member_export_columns_start_with_the_import_columns_in_the_same_order()
        {
            var import = MemberSheet.Import.Columns.Select(c => c.Header).ToList();
            var export = MemberSheet.Export.Columns.Select(c => c.Header).ToList();

            Assert.True(export.Count > import.Count);
            Assert.Equal(import, export.Take(import.Count).ToList());
        }

        [Fact]
        public void Member_export_only_columns_are_read_only()
        {
            // Read-only columns have no Parse, which is what lets an exported file be
            // edited and re-imported without first deleting the ID and date columns.
            var importCount = MemberSheet.Import.Columns.Count;
            var extras = MemberSheet.Export.Columns.Skip(importCount);

            Assert.All(extras, c => Assert.Null(c.Parse));
            Assert.Contains(MemberSheet.Export.Columns, c => c.Header == "Mã hội viên");
        }

        [Fact]
        public void Member_required_columns_are_name_and_phone()
        {
            var required = MemberSheet.Import.Columns.Where(c => c.IsRequired).Select(c => c.Header).ToList();
            Assert.Equal(new[] { "Họ và tên", "Số điện thoại" }, required);
        }

        [Fact]
        public void Enum_columns_render_the_Vietnamese_label_not_the_number()
        {
            var statusColumn = MemberSheet.Export.Columns.Single(c => c.Header == "Trạng thái");
            var row = new MemberRow { Status = MemberStatus.Suspended };

            Assert.Equal("Tạm ngưng", statusColumn.Get(row));
        }

        [Fact]
        public void Nullable_enum_columns_render_empty_rather_than_the_word_null()
        {
            var genderColumn = MemberSheet.Export.Columns.Single(c => c.Header == "Giới tính");
            Assert.Equal("", genderColumn.Get(new MemberRow { Gender = null }));
        }

        [Fact]
        public void Enum_columns_advertise_their_allowed_values_for_the_template_dropdown()
        {
            var statusColumn = MemberSheet.Import.Columns.Single(c => c.Header == "Trạng thái");
            Assert.Equal(new[] { "Hoạt động", "Tạm ngưng", "Hết hạn" }, statusColumn.AllowedValues);
        }

        [Fact]
        public void Invoice_export_shows_the_member_name_rather_than_a_raw_guid()
        {
            var headers = InvoiceSheet.Export.Columns.Select(c => c.Header).ToList();
            Assert.Contains("Hội viên", headers);
            Assert.DoesNotContain("MemberId", headers);
        }
    }
}
