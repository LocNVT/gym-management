namespace gym_management_server.Infrastructure.Excel
{
    public sealed class ReadResult<T>
    {
        public ReadResult(IReadOnlyList<T> rows, IReadOnlyList<RowError> errors, IReadOnlyList<int> rowNumbers)
        {
            if (rows.Count != rowNumbers.Count)
                throw new ArgumentException(
                    $"{nameof(rows)} and {nameof(rowNumbers)} must be the same length ({rows.Count} vs {rowNumbers.Count}).");

            Rows = rows;
            Errors = errors;
            RowNumbers = rowNumbers;
        }

        public IReadOnlyList<T> Rows { get; }
        public IReadOnlyList<RowError> Errors { get; }

        /// <summary>
        /// The true 1-based Excel row each entry in <see cref="Rows"/> came from, in the same
        /// order as <see cref="Rows"/>. A blank or whitespace-only row is skipped silently (no
        /// error, no entry in <see cref="Rows"/>) by the reader, so a caller that instead
        /// recomputes a row number as "index + 2" drifts by one for every blank row that came
        /// before it in the file. Use this list, not an offset, whenever a kept row's own error
        /// needs to point at the right physical Excel row (e.g. a cross-row duplicate check run
        /// after the reader).
        /// </summary>
        public IReadOnlyList<int> RowNumbers { get; }

        public bool IsClean => Errors.Count == 0;
    }
}
