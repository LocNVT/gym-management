using System.Globalization;
using System.IO;
using ClosedXML.Excel;
using gym_management_server.Infrastructure.Excel;
using Xunit;

namespace gym_management_server.Tests.Unit
{
    public class ExcelReaderTests
    {
        private sealed class Row
        {
            public string Name { get; set; } = "";
            public int Age { get; set; }
            public Guid Id { get; set; }   // read-only column
        }

        private static SheetDefinition<Row> Sheet => new("Dữ liệu", new[]
        {
            new ExcelColumn<Row>("Tên", r => r.Name, isRequired: true,
                parse: (r, v) => r.Name = v.Trim()),
            new ExcelColumn<Row>("Tuổi", r => r.Age,
                parse: (r, v) => r.Age = int.TryParse(v, out var n)
                    ? n
                    : throw new FormatException("Tuổi phải là số nguyên.")),
            new ExcelColumn<Row>("Mã", r => r.Id),   // no parse -> read-only
        });

        private static Stream Build(params string[][] rows)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Dữ liệu");
            for (var r = 0; r < rows.Length; r++)
                for (var c = 0; c < rows[r].Length; c++)
                    ws.Cell(r + 1, c + 1).Value = rows[r][c];

            var stream = new MemoryStream();
            wb.SaveAs(stream);
            stream.Position = 0;
            return stream;
        }

        [Fact]
        public void Reads_clean_rows()
        {
            var result = ExcelReader.Read(
                Build(["Tên", "Tuổi"], ["Nguyễn Văn A", "30"], ["Trần Thị B", "25"]), Sheet);

            Assert.True(result.IsClean);
            Assert.Equal(2, result.Rows.Count);
            Assert.Equal("Nguyễn Văn A", result.Rows[0].Name);
            Assert.Equal(25, result.Rows[1].Age);
        }

        [Fact]
        public void Reports_the_excel_row_number_the_user_sees()
        {
            // Header is row 1, so the second data row is Excel row 3.
            var result = ExcelReader.Read(
                Build(["Tên", "Tuổi"], ["Hợp lệ", "30"], ["Sai", "ba mươi"]), Sheet);

            var error = Assert.Single(result.Errors);
            Assert.Equal(3, error.RowNumber);
            Assert.Equal("Tuổi", error.ColumnHeader);
            Assert.Contains("số nguyên", error.Message);
        }

        [Fact]
        public void Collects_every_error_rather_than_stopping_at_the_first()
        {
            var result = ExcelReader.Read(
                Build(["Tên", "Tuổi"], ["", "x"], ["", "y"]), Sheet);

            Assert.Equal(4, result.Errors.Count);   // 2 rows x (missing name + bad age)
        }

        [Fact]
        public void A_missing_required_column_is_one_file_level_error()
        {
            var result = ExcelReader.Read(Build(["Tuổi"], ["30"], ["40"]), Sheet);

            var error = Assert.Single(result.Errors);
            Assert.Equal(1, error.RowNumber);
            Assert.Contains("Tên", error.Message);
        }

        [Fact]
        public void Columns_may_be_reordered_or_omitted()
        {
            var result = ExcelReader.Read(Build(["Tuổi", "Tên"], ["30", "Nguyễn Văn A"]), Sheet);

            Assert.True(result.IsClean);
            Assert.Equal("Nguyễn Văn A", result.Rows[0].Name);
            Assert.Equal(30, result.Rows[0].Age);
        }

        [Fact]
        public void Read_only_columns_present_in_the_file_are_ignored_not_rejected()
        {
            // This is what lets a freshly exported file be edited and re-imported as-is.
            var result = ExcelReader.Read(
                Build(["Tên", "Tuổi", "Mã"], ["Nguyễn Văn A", "30", "không-phải-guid"]), Sheet);

            Assert.True(result.IsClean);
            Assert.Equal(Guid.Empty, result.Rows[0].Id);
        }

        [Fact]
        public void Blank_rows_are_skipped_silently()
        {
            var result = ExcelReader.Read(
                Build(["Tên", "Tuổi"], ["Nguyễn Văn A", "30"], ["", ""], ["Trần Thị B", "25"]), Sheet);

            Assert.True(result.IsClean);
            Assert.Equal(2, result.Rows.Count);
        }

        [Fact]
        public void A_file_over_the_row_limit_is_one_file_level_error()
        {
            var rows = new List<string[]> { new[] { "Tên", "Tuổi" } };
            for (var i = 0; i <= ExcelReader.MaxRows; i++) rows.Add(new[] { $"HV{i}", "30" });

            var result = ExcelReader.Read(Build(rows.ToArray()), Sheet);

            Assert.Contains(result.Errors, e => e.Message.Contains("vượt giới hạn"));
            Assert.Empty(result.Rows);
        }

        [Fact]
        public void Whitespace_only_rows_are_skipped_silently()
        {
            // A row that "looks" blank in Excel but actually holds a stray space in a cell must
            // not turn into a phantom row of default values.
            var result = ExcelReader.Read(
                Build(["Tên", "Tuổi"], ["Nguyễn Văn A", "30"], [" ", "  "], ["Trần Thị B", "25"]), Sheet);

            Assert.True(result.IsClean);
            Assert.Equal(2, result.Rows.Count);
        }

        [Fact]
        public void A_duplicate_header_is_one_file_level_error()
        {
            var result = ExcelReader.Read(
                Build(["Tên", "Tuổi", "Tên"], ["Nguyễn Văn A", "30", "A"]), Sheet);

            var error = Assert.Single(result.Errors);
            Assert.Equal(1, error.RowNumber);
            Assert.Contains("Tên", error.Message);
            Assert.Contains("lặp lại", error.Message);
            Assert.Empty(result.Rows);
        }

        private sealed class DateRow
        {
            public DateTime When { get; set; }
        }

        private static SheetDefinition<DateRow> DateSheet => new("Ngày", new[]
        {
            new ExcelColumn<DateRow>("Ngày", r => r.When, isRequired: true,
                parse: (r, v) => r.When = DateTime.ParseExact(v, "yyyy-MM-dd", CultureInfo.InvariantCulture)),
        });

        [Fact]
        public void A_date_cell_is_read_the_same_regardless_of_its_display_format()
        {
            // A date typed via Excel's date picker is stored as a serial number; its on-screen
            // rendering depends on the cell's number format and the workbook's culture (e.g.
            // US "9/22/2026" vs VN "22/09/2026"). The reader must hand the parser the same,
            // unambiguous text no matter which display format the cell happens to carry.
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Ngày");
            ws.Cell(1, 1).Value = "Ngày";
            var cell = ws.Cell(2, 1);
            cell.Value = new DateTime(2026, 9, 22);
            cell.Style.NumberFormat.Format = "M/d/yyyy"; // would render "9/22/2026"

            var stream = new MemoryStream();
            wb.SaveAs(stream);
            stream.Position = 0;

            var result = ExcelReader.Read(stream, DateSheet);

            Assert.True(result.IsClean);
            Assert.Equal(new DateTime(2026, 9, 22), result.Rows[0].When);
        }

        private sealed class NumberRow
        {
            public decimal Amount { get; set; }
        }

        private static SheetDefinition<NumberRow> NumberSheet => new("Số", new[]
        {
            new ExcelColumn<NumberRow>("Số tiền", r => r.Amount, isRequired: true,
                parse: (r, v) =>
                {
                    if (v.Contains(',')) throw new FormatException("Chuỗi số không được chứa dấu phẩy.");
                    r.Amount = decimal.Parse(v, CultureInfo.InvariantCulture);
                }),
        });

        [Fact]
        public void A_number_cell_with_a_thousands_format_does_not_carry_a_group_separator()
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Số");
            ws.Cell(1, 1).Value = "Số tiền";
            var cell = ws.Cell(2, 1);
            cell.Value = 1234;
            cell.Style.NumberFormat.Format = "#,##0"; // would render "1,234"

            var stream = new MemoryStream();
            wb.SaveAs(stream);
            stream.Position = 0;

            var result = ExcelReader.Read(stream, NumberSheet);

            Assert.True(result.IsClean);
            Assert.Equal(1234m, result.Rows[0].Amount);
        }
    }
}
