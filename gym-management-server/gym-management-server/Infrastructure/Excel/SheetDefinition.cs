namespace gym_management_server.Infrastructure.Excel
{
    public sealed class SheetDefinition<T>
    {
        public SheetDefinition(string sheetName, IReadOnlyList<ExcelColumn<T>> columns)
        {
            SheetName = sheetName;
            Columns = columns;
        }

        public string SheetName { get; }
        public IReadOnlyList<ExcelColumn<T>> Columns { get; }

        /// <summary>Returns a new definition with extra columns appended. Does not mutate this one.</summary>
        public SheetDefinition<T> Plus(params ExcelColumn<T>[] extra) =>
            new(SheetName, Columns.Concat(extra).ToList());
    }
}
