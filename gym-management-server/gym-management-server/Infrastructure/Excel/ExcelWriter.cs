using ClosedXML.Excel;

namespace gym_management_server.Infrastructure.Excel
{
    public class ExcelRowLimitExceededException : Exception
    {
        public ExcelRowLimitExceededException(int actual)
            : base($"Kết quả có {actual:N0} dòng, vượt giới hạn {ExcelWriter.MaxRows:N0} dòng mỗi lần xuất. Vui lòng thu hẹp bộ lọc.")
            => Actual = actual;

        public int Actual { get; }
    }

    public static class ExcelWriter
    {
        /// <summary>ClosedXML builds the whole workbook in memory, so an unbounded export
        /// is a way to exhaust the server's RAM with one request.</summary>
        public const int MaxRows = 50_000;

        public const string ContentType =
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        public static byte[] Write<T>(SheetDefinition<T> sheet, IReadOnlyCollection<T> rows) =>
            Write(new SheetPayload<T>(sheet, rows));

        public static byte[] Write(params ISheetPayload[] payloads)
        {
            var total = payloads.Sum(p => p.RowCount);
            if (total > MaxRows) throw new ExcelRowLimitExceededException(total);

            using var workbook = new XLWorkbook();
            foreach (var payload in payloads) payload.WriteTo(workbook);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
