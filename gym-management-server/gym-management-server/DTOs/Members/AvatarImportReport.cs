namespace gym_management_server.DTOs.Members
{
    public class AvatarImportResultOutput
    {
        public string FileName { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public bool Success { get; set; }
        public string? Message { get; set; }

        public static AvatarImportResultOutput Ok(string fileName, string? phoneNumber) => new()
        {
            FileName = fileName,
            PhoneNumber = phoneNumber,
            Success = true,
            Message = "Cập nhật ảnh thành công."
        };

        public static AvatarImportResultOutput Fail(string fileName, string? phoneNumber, string message) => new()
        {
            FileName = fileName,
            PhoneNumber = phoneNumber,
            Success = false,
            Message = message
        };
    }

    public class AvatarImportReport
    {
        public List<AvatarImportResultOutput> Results { get; set; } = new();
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
    }
}
