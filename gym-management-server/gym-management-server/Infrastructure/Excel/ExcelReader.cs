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

            using var workbook = new XLWorkbook(stream);
            var ws = workbook.Worksheets.FirstOrDefault(w => w.Name == sheet.SheetName)
                     ?? workbook.Worksheets.First();

            // Only columns with a Parse can be filled from the file; the rest are export-only.
            var writable = sheet.Columns.Where(c => c.Parse is not null).ToList();

            // Match the header by name, so the user may reorder or drop columns.
            var headerRow = ws.Row(1);
            var positions = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var cell in headerRow.CellsUsed())
            {
                var name = cell.GetString().Trim();
                if (name.Length > 0) positions.TryAdd(name, cell.Address.ColumnNumber);
            }

            foreach (var column in writable.Where(c => c.IsRequired && !positions.ContainsKey(c.Header)))
                errors.Add(new RowError(1, column.Header, $"Thiếu cột bắt buộc \"{column.Header}\"."));

            if (errors.Count > 0) return new ReadResult<T>(rows, errors);

            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
            if (lastRow - 1 > MaxRows)
            {
                errors.Add(new RowError(1, null,
                    $"File có {lastRow - 1:N0} dòng, vượt giới hạn {MaxRows:N0} dòng mỗi lần nhập."));
                return new ReadResult<T>(rows, errors);
            }

            for (var r = 2; r <= lastRow; r++)
            {
                var excelRow = ws.Row(r);
                if (excelRow.IsEmpty()) continue;

                var item = new T();
                var rowHadError = false;

                foreach (var column in writable)
                {
                    if (!positions.TryGetValue(column.Header, out var columnNumber)) continue;

                    var raw = ws.Cell(r, columnNumber).GetFormattedString().Trim();

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

                if (!rowHadError) rows.Add(item);
            }

            return new ReadResult<T>(rows, errors);
        }
    }
}
