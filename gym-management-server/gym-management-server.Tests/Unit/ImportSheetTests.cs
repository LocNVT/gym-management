using System.Linq;
using gym_management_server.DTOs.Export;
using gym_management_server.Entities.Enums;
using gym_management_server.Infrastructure.Excel;
using gym_management_server.Services.Members;
using gym_management_server.Services.ServicePackages;
using Xunit;

namespace gym_management_server.Tests.Unit
{
    public class ImportSheetTests
    {
        private static void Parse(string header, MemberRow row, string value) =>
            MemberSheet.Import.Columns.Single(c => c.Header == header).Parse!(row, value);

        private static void ParsePackage(string header, ServicePackageRow row, string value) =>
            ServicePackageSheet.Import.Columns.Single(c => c.Header == header).Parse!(row, value);

        [Fact]
        public void Every_import_column_can_be_parsed()
        {
            Assert.All(MemberSheet.Import.Columns, c => Assert.NotNull(c.Parse));
            Assert.All(ServicePackageSheet.Import.Columns, c => Assert.NotNull(c.Parse));
        }

        [Fact]
        public void Export_only_extras_stay_read_only_so_a_re_exported_file_can_be_re_imported()
        {
            // Export = Import.Plus(extras); the extras must keep Parse == null, otherwise a
            // freshly exported file (which carries id/created-date columns) can no longer be
            // re-imported because those columns would suddenly be writable-but-unmapped.
            var extraCount = MemberSheet.Export.Columns.Count - MemberSheet.Import.Columns.Count;
            Assert.True(extraCount > 0);
            Assert.All(MemberSheet.Export.Columns.Skip(MemberSheet.Import.Columns.Count),
                c => Assert.Null(c.Parse));
        }

        [Fact]
        public void Dates_are_read_in_Vietnamese_day_first_order()
        {
            var row = new MemberRow();
            Parse("Ngày sinh", row, "09/03/1995");
            Assert.Equal(new DateTime(1995, 3, 9), row.DateOfBirth);
        }

        [Fact]
        public void An_unparseable_date_explains_the_expected_format()
        {
            var ex = Assert.Throws<FormatException>(() => Parse("Ngày sinh", new MemberRow(), "1995-03-09x"));
            Assert.Contains("dd/MM/yyyy", ex.Message);
        }

        [Fact]
        public void A_date_cell_carrying_a_time_component_from_the_reader_still_parses()
        {
            // ExcelReader emits "yyyy-MM-ddTHH:mm:ss" for a date cell whose time-of-day is
            // non-zero; the date parser must accept that exact shape, not just the
            // text-typed dd/MM/yyyy formats.
            var row = new MemberRow();
            Parse("Ngày sinh", row, "1995-03-09T00:00:01");
            Assert.Equal(new DateTime(1995, 3, 9, 0, 0, 1), row.DateOfBirth);
        }

        [Fact]
        public void Enum_cells_accept_the_Vietnamese_label_in_any_casing()
        {
            var row = new MemberRow();
            Parse("Trạng thái", row, "  tạm ngưng ");
            Assert.Equal(MemberStatus.Suspended, row.Status);

            Parse("Giới tính", row, "Nữ");
            Assert.Equal(Gender.Female, row.Gender);
        }

        [Fact]
        public void An_unknown_enum_cell_lists_the_accepted_values()
        {
            var ex = Assert.Throws<FormatException>(() => Parse("Trạng thái", new MemberRow(), "Đang nghỉ"));
            Assert.Contains("Hoạt động", ex.Message);
            Assert.Contains("Tạm ngưng", ex.Message);
        }

        [Theory]
        [InlineData("1.500.000", 1500000)]
        [InlineData("1,500,000", 1500000)]
        [InlineData("1500000", 1500000)]
        public void Prices_typed_as_text_tolerate_the_thousands_separators_real_spreadsheets_contain(
            string input, decimal expected)
        {
            var row = new ServicePackageRow();
            ParsePackage("Đơn giá", row, input);
            Assert.Equal(expected, row.Price);
        }

        [Fact]
        public void A_numeric_cell_holding_a_decimal_is_not_mistaken_for_a_thousands_separated_integer()
        {
            // This is the regression that matters most: ExcelReader now hands numeric cells
            // to Parse as culture-invariant text where "." is the DECIMAL point (e.g.
            // "1234.5"), never a thousands separator. The old brief's Money() stripped "."
            // and "," unconditionally, which would silently turn 1234.5 into 12345 — a
            // 10x error on money, with no exception and no row error. The invariant parse
            // must be tried first.
            var row = new ServicePackageRow();
            ParsePackage("Đơn giá", row, "1234.5");
            Assert.Equal(1234.5m, row.Price);
        }

        [Fact]
        public void A_number_cell_in_scientific_notation_still_parses_as_money()
        {
            // double.ToString(InvariantCulture) emits scientific notation outside roughly
            // [1e-5, 1e15); the money parser must accept it rather than fail loudly claiming
            // "not a valid number" for what is in fact a perfectly good large price.
            var row = new ServicePackageRow();
            ParsePackage("Đơn giá", row, "1E+16");
            Assert.Equal(10_000_000_000_000_000m, row.Price);
        }

        [Fact]
        public void An_integer_cell_in_scientific_notation_still_parses()
        {
            var row = new ServicePackageRow();
            ParsePackage("Số ngày", row, "1E+2");
            Assert.Equal(100, row.DurationDays);
        }

        [Theory]
        [InlineData("Có", true)]
        [InlineData("không", false)]
        [InlineData("1", true)]
        [InlineData("false", false)]
        public void Booleans_accept_the_Vietnamese_words_and_the_usual_literals(string input, bool expected)
        {
            var row = new ServicePackageRow();
            ParsePackage("Đang áp dụng", row, input);
            Assert.Equal(expected, row.IsActive);
        }
    }
}
