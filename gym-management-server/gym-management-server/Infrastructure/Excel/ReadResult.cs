namespace gym_management_server.Infrastructure.Excel
{
    public sealed class ReadResult<T>
    {
        public ReadResult(IReadOnlyList<T> rows, IReadOnlyList<RowError> errors)
        {
            Rows = rows;
            Errors = errors;
        }

        public IReadOnlyList<T> Rows { get; }
        public IReadOnlyList<RowError> Errors { get; }
        public bool IsClean => Errors.Count == 0;
    }
}
