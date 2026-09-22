namespace gym_management_server.Infrastructure.Excel
{
    /// <summary>
    /// One spreadsheet column. The same declaration drives the export file, the import
    /// template and the upload parser, so the three can never drift apart.
    /// A column with a null <see cref="Parse"/> is read-only: it is written on export and
    /// ignored on import, which is what lets a freshly exported file be edited and
    /// re-imported without deleting the ID and timestamp columns first.
    /// </summary>
    public sealed class ExcelColumn<T>
    {
        public ExcelColumn(
            string header,
            Func<T, object?> get,
            string? format = null,
            double width = 18,
            bool isRequired = false,
            Action<T, string>? parse = null,
            IReadOnlyList<string>? allowedValues = null)
        {
            Header = header;
            Get = get;
            Format = format;
            Width = width;
            IsRequired = isRequired;
            Parse = parse;
            AllowedValues = allowedValues;
        }

        public string Header { get; }
        public Func<T, object?> Get { get; }
        public string? Format { get; }
        public double Width { get; }
        public bool IsRequired { get; }
        public Action<T, string>? Parse { get; }
        public IReadOnlyList<string>? AllowedValues { get; }
    }
}
