using ClosedXML.Excel;

namespace gym_management_server.Infrastructure.Excel
{
    /// <summary>
    /// Builds the downloadable import template for a sheet: the header row (required
    /// columns marked with a trailing " *"), one example row so the expected format is
    /// obvious without reading a manual, and a dropdown on every column that has
    /// <see cref="ExcelColumn{T}.AllowedValues"/>.
    /// </summary>
    public static class ExcelTemplateWriter
    {
        private const int ValidatedRows = 500;

        public static byte[] Write<T>(SheetDefinition<T> sheet)
        {
            // Only columns the user can fill in belong on a blank template; a read-only,
            // export-only column (id, created date, ...) has nothing to demonstrate here.
            var columns = sheet.Columns.Where(c => c.Parse is not null).ToList();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add(sheet.SheetName);

            for (var c = 0; c < columns.Count; c++)
            {
                var column = columns[c];
                var header = ws.Cell(1, c + 1);
                header.Value = column.Header + (column.IsRequired ? " *" : "");
                header.Style.Font.Bold = true;
                header.Style.Fill.BackgroundColor =
                    column.IsRequired ? XLColor.LightSalmon : XLColor.LightGray;
                ws.Column(c + 1).Width = column.Width;

                // One example row, so the expected format is obvious without reading a manual.
                ws.Cell(2, c + 1).Value = Example(column);

                if (column.AllowedValues is { Count: > 0 } allowed)
                    ws.Range(2, c + 1, ValidatedRows, c + 1)
                      .CreateDataValidation()
                      .List(string.Join(",", allowed), inCellDropdown: true);
            }

            ws.Row(2).Style.Font.Italic = true;
            ws.Row(2).Style.Font.FontColor = XLColor.Gray;
            ws.SheetView.FreezeRows(1);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private static string Example<T>(ExcelColumn<T> column)
        {
            if (column.AllowedValues is { Count: > 0 } allowed) return allowed[0];
            if (column.Format is not null && column.Format.Contains('/')) return "01/01/2026";
            if (column.Format is not null && column.Format.Contains('#')) return "1000000";
            return $"(ví dụ {column.Header.ToLowerInvariant()})";
        }
    }
}
