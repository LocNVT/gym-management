using System.Linq;
using gym_management_server.Infrastructure.Excel;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace gym_management_server.Services.Import
{
    /// <summary>
    /// Both import services wrap their commit in a try/catch around <see cref="DbUpdateException"/>
    /// to cover a race between the pre-commit duplicate check and the actual write (a concurrent
    /// import landing in between). But <see cref="DbUpdateException"/> is the same type SaveChanges
    /// throws for a string-length constraint violation (which <c>CellParse.Text</c> is meant to
    /// have already ruled out, but a defect there -- or a column it doesn't cover -- would still
    /// surface here) and for any other write failure the database happens to raise. Only a
    /// duplicate-key violation is actually a "someone else changed the data" story; the other two
    /// must not be reported with that same claim.
    /// </summary>
    public static class ImportConflictMessage
    {
        // 2601 "Cannot insert duplicate key row in object ... with unique index" and 2627
        // "Violation of %ls constraint" cover both a unique index and an explicit unique
        // constraint -- the two shapes a real duplicate-key race can take in SQL Server.
        private static readonly int[] DuplicateKeyErrorNumbers = { 2601, 2627 };

        // 8152 (older engines, no column name) and 2628 (current engines, names the column) are
        // both "String or binary data would be truncated" -- a length violation, not a race.
        private static readonly int[] TruncationErrorNumbers = { 8152, 2628 };

        public static RowError For(DbUpdateException ex)
        {
            if (ex.InnerException is SqlException sql)
            {
                if (DuplicateKeyErrorNumbers.Contains(sql.Number))
                    return new RowError(1, null,
                        "Dữ liệu đã thay đổi trong khi xử lý file (một dòng đã được người khác nhập trước đó). " +
                        "Vui lòng kiểm tra lại và thử lại.");

                if (TruncationErrorNumbers.Contains(sql.Number))
                    return new RowError(1, null,
                        "Một giá trị trong file vượt quá độ dài cho phép của cột tương ứng trong hệ thống. " +
                        "Vui lòng rút ngắn nội dung và thử lại.");
            }

            // Anything else -- including a provider that doesn't surface SqlException at all
            // (e.g. the SQLite-backed test doubles that simulate this path with a bare
            // DbUpdateException) -- is a write failure whose exact cause this catch block cannot
            // determine. Say so rather than asserting a specific cause we cannot verify.
            return new RowError(1, null,
                "Không thể ghi dữ liệu vào hệ thống (nguyên nhân không xác định). " +
                "Vui lòng thử lại; nếu lỗi lặp lại, hãy liên hệ quản trị viên.");
        }
    }
}
