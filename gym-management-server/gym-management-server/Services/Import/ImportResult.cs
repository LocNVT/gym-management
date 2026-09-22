using gym_management_server.Infrastructure.Excel;

namespace gym_management_server.Services.Import
{
    /// <summary>
    /// <paramref name="Committed"/> is false for a dry run and for any run that found errors.
    /// <paramref name="ImportedRows"/> is 0 unless <paramref name="Committed"/> is true —
    /// import is all-or-nothing.
    /// </summary>
    public record ImportResult(
        int TotalRows,
        int ImportedRows,
        bool Committed,
        IReadOnlyList<RowError> Errors)
    {
        public bool IsClean => Errors.Count == 0;
    }
}
