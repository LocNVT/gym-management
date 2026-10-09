namespace gym_management_server.Infrastructure.Images
{
    /// <summary>
    /// Checks a file's actual bytes against the image format its extension claims, instead of
    /// trusting the extension alone - see docs/ImprovementPlan.md mục 3.
    /// </summary>
    public static class ImageFileValidator
    {
        public const long MaxBytes = 5 * 1024 * 1024;

        public static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        public static async Task<bool> LooksLikeImageAsync(IFormFile file, string extension)
        {
            await using var stream = file.OpenReadStream();
            var header = new byte[12];
            var read = await stream.ReadAsync(header.AsMemory(0, header.Length));
            return LooksLikeImage(header.AsSpan(0, read), extension);
        }

        public static bool LooksLikeImage(ReadOnlySpan<byte> header, string extension) => extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            ".png" => header.Length >= 8 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47
                && header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A,
            ".gif" => header.Length >= 4 && header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x38,
            ".webp" => header.Length >= 12
                && header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 // "RIFF"
                && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50, // "WEBP"
            _ => false,
        };
    }
}
