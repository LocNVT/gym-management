namespace gym_management_server.DTOs.Auth
{
    public class ForgotPasswordInput
    {
        public string Email { get; set; } = null!;
    }

    public class VerifyOtpInput
    {
        public string Email { get; set; } = null!;
        public string OtpCode { get; set; } = null!;
    }

    public class ResetPasswordInput
    {
        public string Email { get; set; } = null!;
        public string OtpCode { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
    }
}
