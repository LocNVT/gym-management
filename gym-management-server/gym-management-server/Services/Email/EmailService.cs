using MailKit.Net.Smtp;
using MimeKit;

namespace gym_management_server.Services.Email
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendOtpEmailAsync(string toEmail, string otpCode)
        {
            var smtpSettings = _config.GetSection("Smtp");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                smtpSettings["SenderName"] ?? "Gym Management",
                smtpSettings["SenderEmail"]));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = "Mã OTP xác thực - Gym Management";

            message.Body = new TextPart("html")
            {
                Text = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 500px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #333;'>Xác thực tài khoản</h2>
                        <p>Mã OTP của bạn là:</p>
                        <div style='background: #f0f0f0; padding: 15px; text-align: center; font-size: 32px; font-weight: bold; letter-spacing: 8px; border-radius: 8px;'>
                            {otpCode}
                        </div>
                        <p style='color: #666; margin-top: 15px;'>Mã có hiệu lực trong <strong>5 phút</strong>.</p>
                        <p style='color: #999; font-size: 12px;'>Nếu bạn không yêu cầu mã này, vui lòng bỏ qua email này.</p>
                    </div>"
            };

            using var client = new SmtpClient();
            await client.ConnectAsync(
                smtpSettings["Host"],
                int.Parse(smtpSettings["Port"] ?? "587"),
                MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(
                smtpSettings["SenderEmail"],
                smtpSettings["Password"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
