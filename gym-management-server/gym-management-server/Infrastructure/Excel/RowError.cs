namespace gym_management_server.Infrastructure.Excel
{
    /// <summary>
    /// One problem found in an uploaded file. <paramref name="RowNumber"/> is the row number
    /// as Excel shows it to the user (header = 1), so they can go straight to the cell.
    /// A null <paramref name="ColumnHeader"/> means the problem is with the row or the file
    /// as a whole rather than one cell.
    /// </summary>
    public record RowError(int RowNumber, string? ColumnHeader, string Message);
}
