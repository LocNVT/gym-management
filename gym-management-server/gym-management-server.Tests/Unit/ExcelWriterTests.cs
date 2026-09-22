using System.IO;
using ClosedXML.Excel;
using gym_management_server.Infrastructure.Excel;
using Xunit;

namespace gym_management_server.Tests.Unit
{
    public class ExcelWriterTests
    {
        private sealed class Row
        {
            public string Name { get; set; } = "";
            public decimal Amount { get; set; }
            public DateTime? When { get; set; }
        }

        private static SheetDefinition<Row> Sheet => new("Thử nghiệm", new[]
        {
            new ExcelColumn<Row>("Tên", r => r.Name, isRequired: true),
            new ExcelColumn<Row>("Số tiền", r => r.Amount, format: "#,##0"),
            new ExcelColumn<Row>("Ngày", r => r.When, format: "dd/MM/yyyy"),
        });

        private static IXLWorksheet Open(byte[] bytes, string sheetName)
            => new XLWorkbook(new MemoryStream(bytes)).Worksheet(sheetName);

        [Fact]
        public void Writes_the_headers_in_declaration_order()
        {
            var bytes = ExcelWriter.Write(Sheet, new[] { new Row { Name = "A" } });
            var ws = Open(bytes, "Thử nghiệm");

            Assert.Equal("Tên", ws.Cell(1, 1).GetString());
            Assert.Equal("Số tiền", ws.Cell(1, 2).GetString());
            Assert.Equal("Ngày", ws.Cell(1, 3).GetString());
        }

        [Fact]
        public void Writes_one_row_per_item_starting_at_row_2()
        {
            var rows = new[]
            {
                new Row { Name = "Nguyễn Văn A", Amount = 1500000m, When = new DateTime(2026, 3, 9) },
                new Row { Name = "Trần Thị B",   Amount = 250000m,  When = null },
            };

            var ws = Open(ExcelWriter.Write(Sheet, rows), "Thử nghiệm");

            Assert.Equal("Nguyễn Văn A", ws.Cell(2, 1).GetString());
            Assert.Equal(1500000m, ws.Cell(2, 2).GetValue<decimal>());
            Assert.Equal(new DateTime(2026, 3, 9), ws.Cell(2, 3).GetDateTime());
            Assert.Equal("Trần Thị B", ws.Cell(3, 1).GetString());
            Assert.True(ws.Cell(3, 3).IsEmpty());   // null must be a blank cell, not the text "null"
            Assert.True(ws.Cell(4, 1).IsEmpty());
        }

        [Fact]
        public void Applies_the_declared_number_format()
        {
            var ws = Open(ExcelWriter.Write(Sheet, new[] { new Row { Amount = 1m } }), "Thử nghiệm");
            Assert.Equal("#,##0", ws.Cell(2, 2).Style.NumberFormat.Format);
        }

        [Fact]
        public void Write_refuses_more_than_the_row_limit()
        {
            var tooMany = Enumerable.Range(0, ExcelWriter.MaxRows + 1).Select(_ => new Row()).ToList();
            Assert.Throws<ExcelRowLimitExceededException>(() => ExcelWriter.Write(Sheet, tooMany));
        }

        [Fact]
        public void Write_puts_each_payload_on_its_own_sheet()
        {
            var second = new SheetDefinition<Row>("Chi tiết", new[] { new ExcelColumn<Row>("Tên", r => r.Name) });

            var bytes = ExcelWriter.Write(
                new SheetPayload<Row>(Sheet, new[] { new Row { Name = "A" } }),
                new SheetPayload<Row>(second, new[] { new Row { Name = "B" } }));

            using var wb = new XLWorkbook(new MemoryStream(bytes));
            Assert.Equal(2, wb.Worksheets.Count);
            Assert.Equal("B", wb.Worksheet("Chi tiết").Cell(2, 1).GetString());
        }

        [Fact]
        public void Plus_appends_columns_and_leaves_the_original_untouched()
        {
            var extended = Sheet.Plus(new ExcelColumn<Row>("Ghi chú", _ => "x"));

            Assert.Equal(3, Sheet.Columns.Count);
            Assert.Equal(4, extended.Columns.Count);
            Assert.Equal("Ghi chú", extended.Columns[3].Header);
        }
    }
}
