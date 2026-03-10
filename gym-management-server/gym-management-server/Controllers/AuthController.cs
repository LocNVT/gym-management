using gym_management_server.DTOs.Auth;
using gym_management_server.Services.Auth;
using Microsoft.AspNetCore.Mvc;

namespace gym_management_server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterInput input)
        {
            try
            {
                var result = await _authService.RegisterAsync(input);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginInput input)
        {
            try
            {
                var result = await _authService.LoginAsync(input);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("google")]
        public async Task<IActionResult> GoogleLogin(GoogleLoginInput input)
        {
            try
            {
                var result = await _authService.GoogleLoginAsync(input);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordInput input)
        {
            var result = await _authService.ForgotPasswordAsync(input);
            // Always return OK to prevent email enumeration
            return Ok(new { message = "Nếu email tồn tại, mã OTP đã được gửi." });
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp(VerifyOtpInput input)
        {
            var result = await _authService.VerifyOtpAsync(input);
            return result ? Ok(new { message = "OTP hợp lệ." }) : BadRequest(new { message = "OTP không hợp lệ hoặc đã hết hạn." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordInput input)
        {
            var result = await _authService.ResetPasswordAsync(input);
            return result ? Ok(new { message = "Đổi mật khẩu thành công." }) : BadRequest(new { message = "OTP không hợp lệ hoặc đã hết hạn." });
        }
    }
}
