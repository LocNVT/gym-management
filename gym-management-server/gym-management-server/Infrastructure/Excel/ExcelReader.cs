using System.Globalization;
using ClosedXML.Excel;

namespace gym_management_server.Infrastructure.Excel
{
    public static class ExcelReader
    {
        public const int MaxRows = 10_000;

        public static ReadResult<T> Read<T>(Stream stream, SheetDefinition<T> sheet) where T : new()
        {
            var errors = new List<RowError>();
            var rows = new List<T>();
            var rowNumbers = new List<int>();

            using var workbook = new XLWorkbook(stream);
            var ws = workbook.Worksheets.FirstOrDefault(w => w.Name == sheet.SheetName)
                     ?? workbook.Worksheets.First();

            // Only columns with a Parse can be filled from the file; the rest are export-only.
            var writable = sheet.Columns.Where(c => c.Parse is not null).ToList();

            // Match the header by name, so the user may reorder or drop columns.
            var headerRow = ws.Row(1);
            var positions = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var duplicateHeaders = new List<string>();
            foreach (var cell in headerRow.CellsUsed())
            {
                // ExcelTemplateWriter appends " *" to a required header for the user's
                // benefit; match by the bare name so a template's own file re-imports.
                var name = cell.GetString().Trim().TrimEnd('*').Trim();
                if (name.Length == 0) continue;
                if (!positions.TryAdd(name, cell.Address.ColumnNumber))
                    duplicateHeaders.Add(name);
            }

            // A repeated header silently shadows one of the columns rather than being
            // reported, so make it a loud file-level error instead of dropping data.
            foreach (var name in duplicateHeaders)
                errors.Add(new RowError(1, name, $"Cột \"{name}\" bị lặp lại trong tiêu đề."));

            foreach (var column in writable.Where(c => c.IsRequired && !positions.ContainsKey(c.Header)))
                errors.Add(new RowError(1, column.Header, $"Thiếu cột bắt buộc \"{column.Header}\"."));

            if (errors.Count > 0) return new ReadResult<T>(rows, errors, rowNumbers);

            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
            if (lastRow - 1 > MaxRows)
            {
                errors.Add(new RowError(1, null,
                    $"File có {lastRow - 1:N0} dòng, vượt giới hạn {MaxRows:N0} dòng mỗi lần nhập."));
                return new ReadResult<T>(rows, errors, rowNumbers);
            }

            for (var r = 2; r <= lastRow; r++)
            {
                var excelRow = ws.Row(r);
                if (excelRow.IsEmpty()) continue;

                // IXLRow.IsEmpty() only tells us the row has no used cells at all; a row whose
                // only content is whitespace (" ") still counts as "used" and would otherwise
                // fall through as a phantom row of default values. Read each importable cell
                // once, and if every one of them is blank after trimming, treat the row as
                // blank too and skip it silently, same as a genuinely empty row.
                var cells = new List<(ExcelColumn<T> Column, string Raw)>();
                foreach (var column in writable)
                {
                    if (positions.TryGetValue(column.Header, out var columnNumber))
                        cells.Add((column, CellText(ws.Cell(r, columnNumber))));
                }

                if (cells.All(c => c.Raw.Length == 0)) continue;

                var item = new T();
                var rowHadError = false;

                foreach (var (column, raw) in cells)
                {
                    if (raw.Length == 0)
                    {
                        if (column.IsRequired)
                        {
                            errors.Add(new RowError(r, column.Header, $"\"{column.Header}\" không được để trống."));
                            rowHadError = true;
                        }
                        continue;
                    }

                    try
                    {
                        column.Parse!(item, raw);
                    }
                    catch (Exception ex)
                    {
                        errors.Add(new RowError(r, column.Header, ex.Message));
                        rowHadError = true;
                    }
                }

                if (!rowHadError)
                {
                    rows.Add(item);
                    rowNumbers.Add(r);
                }
            }

            return new ReadResult<T>(rows, errors, rowNumbers);
        }

        /// <summary>
        /// Renders a cell as a culture-invariant, unambiguous string for the column's
        /// <c>Parse</c> delegate to consume. <see cref="IXLCell.GetFormattedString"/> renders
        /// through the cell's display format and the workbook's culture, so the same date or
        /// number can come out differently depending on who authored the file (a US-formatted
        /// date, a thousands-grouped number). Reading by <see cref="IXLCell.DataType"/> instead
        /// gives every consumer the same text regardless of how the cell happens to be
        /// displayed.
        /// </summary>
        private static string CellText(IXLCell cell) => cell.DataType switch
        {
            XLDataType.DateTime => FormatDateTime(cell.GetDateTime()),
            XLDataType.Number => cell.GetDouble().ToString(CultureInfo.InvariantCulture),
            XLDataType.Boolean => cell.GetBoolean() ? "True" : "False",
            _ => cell.GetFormattedString().Trim(),
        };

        // ISO 8601, date-only unless the value actually carries a time component, so a
        // date-only cell doesn't grow a spurious "00:00:00" that a date-only parser would
        // then have to strip back off.
        private static string FormatDateTime(DateTime value) =>
            value.TimeOfDay == TimeSpan.Zero
                ? value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : value.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
    }
}
