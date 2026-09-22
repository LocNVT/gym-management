using System.IO;
using System.Linq;
using ClosedXML.Excel;
using gym_management_server.Infrastructure.Excel;
using gym_management_server.Services.Members;
using Xunit;

namespace gym_management_server.Tests.Unit
{
    public class ExcelTemplateWriterTests
    {
        private static IXLWorksheet Template()
        {
            var bytes = ExcelTemplateWriter.Write(MemberSheet.Import);
            return new XLWorkbook(new MemoryStream(bytes)).Worksheet("Thành viên");
        }

        [Fact]
        public void Template_has_the_import_headers_and_nothing_export_only()
        {
            var ws = Template();
            Assert.Equal("Họ và tên *", ws.Cell(1, 1).GetString());
            Assert.Equal(MemberSheet.Import.Columns.Count, ws.Row(1).CellsUsed().Count());
        }

        [Fact]
        public void Template_includes_one_example_row_so_the_expected_format_is_obvious()
        {
            var ws = Template();
            Assert.False(ws.Cell(2, 1).IsEmpty());
        }

        [Fact]
        public void Enum_columns_get_a_dropdown_listing_the_allowed_values()
        {
            var ws = Template();
            var statusIndex = MemberSheet.Import.Columns
                .Select((c, i) => (c, i)).Single(x => x.c.Header == "Trạng thái").i + 1;

            var validation = ws.Cell(3, statusIndex).GetDataValidation();
            Assert.NotNull(validation);
            Assert.Contains("Hoạt động", validation!.Value);
        }

        [Fact]
        public void A_header_written_by_the_template_is_matched_back_by_the_reader()
        {
            // ExcelTemplateWriter appends " *" to a required header for the user's benefit;
            // ExcelReader must trim it back off, or a template downloaded and filled in by
            // the user would report every required column as missing. This is the actual
            // round trip the feature depends on.
            var bytes = ExcelTemplateWriter.Write(MemberSheet.Import);
            using var stream = new MemoryStream(bytes);

            var result = ExcelReader.Read(stream, MemberSheet.Import);

            Assert.DoesNotContain(result.Errors, e =>
                e.Message.Contains("Thiếu cột bắt buộc"));
        }
    }
}
