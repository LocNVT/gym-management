using ClosedXML.Excel;

namespace gym_management_server.Infrastructure.Excel
{
    public interface ISheetPayload
    {
        int RowCount { get; }
        void WriteTo(XLWorkbook workbook);
    }

    public sealed class SheetPayload<T> : ISheetPayload
    {
        private readonly SheetDefinition<T> _sheet;
        private readonly IReadOnlyCollection<T> _rows;

        public SheetPayload(SheetDefinition<T> sheet, IReadOnlyCollection<T> rows)
        {
            _sheet = sheet;
            _rows = rows;
        }

        public int RowCount => _rows.Count;

        public void WriteTo(XLWorkbook workbook)
        {
            var ws = workbook.Worksheets.Add(_sheet.SheetName);

            for (var c = 0; c < _sheet.Columns.Count; c++)
            {
                var column = _sheet.Columns[c];
                var header = ws.Cell(1, c + 1);
                header.Value = column.Header;
                header.Style.Font.Bold = true;
                header.Style.Fill.BackgroundColor = XLColor.LightGray;
                ws.Column(c + 1).Width = column.Width;
            }

            var r = 2;
            foreach (var row in _rows)
            {
                for (var c = 0; c < _sheet.Columns.Count; c++)
                {
                    var column = _sheet.Columns[c];
                    var cell = ws.Cell(r, c + 1);
                    var value = column.Get(row);

                    if (value is null) continue;   // leave the cell genuinely empty
                    cell.Value = XLCellValue.FromObject(value);
                    if (column.Format is not null) cell.Style.NumberFormat.Format = column.Format;
                }
                r++;
            }

            ws.SheetView.FreezeRows(1);
            ws.RangeUsed()?.SetAutoFilter();
        }
    }
}
