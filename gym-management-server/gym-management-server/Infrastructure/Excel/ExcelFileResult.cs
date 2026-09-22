using Microsoft.AspNetCore.Mvc;

namespace gym_management_server.Infrastructure.Excel
{
    public static class ExcelFileResult
    {
        public static FileContentResult File(byte[] bytes, string baseName) =>
            new(bytes, ExcelWriter.ContentType)
            {
                FileDownloadName = $"{baseName}-{DateTime.UtcNow.AddHours(7):yyyy-MM-dd}.xlsx"
            };
    }
}
