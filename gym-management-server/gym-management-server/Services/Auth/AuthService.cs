using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using gym_management_server.DTOs.Auth;
using gym_management_server.Entities.OtpTokens;
using gym_management_server.Entities.Users;
using gym_management_server.Repositories.OtpTokens;
using gym_management_server.Repositories.Users;
using gym_management_server.Services.Email;
using Google.Apis.Auth;
using Microsoft.IdentityModel.Tokens;

namespace gym_management_server.Services.Auth
{
    public class AuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IOtpTokenRepository _otpTokenRepository;
        private readonly EmailService _emailService;
        private readonly IConfiguration _config;

        public AuthService(
            IUserRepository userRepository,
            IOtpTokenRepository otpTokenRepository,
            EmailService emailService,
            IConfiguration config)
        {
            _userRepository = userRepository;
            _otpTokenRepository = otpTokenRepository;
            _emailService = emailService;
            _config = config;
        }

        public async Task<AuthOutput> RegisterAsync(RegisterInput input)
        {
            var existingUser = await _userRepository.GetByUsernameAsync(input.Username);
            if (existingUser != null)
                throw new Exception("Username đã tồn tại.");

            var existingEmail = await _userRepository.GetByEmailAsync(input.Email);
            if (existingEmail != null)
                throw new Exception("Email đã được sử dụng.");

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = input.Username,
                Email = input.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(input.Password),
                FullName = input.FullName,
                Role = 0,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(user);
            return GenerateAuthOutput(user);
        }

        public async Task<AuthOutput> LoginAsync(LoginInput input)
        {
            var user = await _userRepository.GetByUsernameAsync(input.Username);
            if (user == null || string.IsNullOrEmpty(user.PasswordHash))
                throw new Exception("Tên đăng nhập hoặc mật khẩu không đúng.");

            if (!BCrypt.Net.BCrypt.Verify(input.Password, user.PasswordHash))
                throw new Exception("Tên đăng nhập hoặc mật khẩu không đúng.");

            return GenerateAuthOutput(user);
        }

        public async Task<AuthOutput> GoogleLoginAsync(GoogleLoginInput input)
        {
            var googleClientId = _config["Google:ClientId"];
            var payload = await GoogleJsonWebSignature.ValidateAsync(input.IdToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { googleClientId }
                });

            var user = await _userRepository.GetByGoogleIdAsync(payload.Subject);
            if (user == null)
            {
                // Check if email already exists
                user = await _userRepository.GetByEmailAsync(payload.Email);
                if (user != null)
                {
                    // Link Google account
                    user.GoogleId = payload.Subject;
                    user.UpdatedAt = DateTime.UtcNow;
                    await _userRepository.UpdateAsync(user);
                }
                else
                {
                    // Create new user
                    user = new User
                    {
                        Id = Guid.NewGuid(),
                        Username = payload.Email,
                        Email = payload.Email,
                        FullName = payload.Name ?? payload.Email,
                        GoogleId = payload.Subject,
                        Role = 0,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _userRepository.AddAsync(user);
                }
            }

            return GenerateAuthOutput(user);
        }

        public async Task<bool> ForgotPasswordAsync(ForgotPasswordInput input)
        {
            var user = await _userRepository.GetByEmailAsync(input.Email);
            if (user == null) return false;

            var otpCode = new Random().Next(100000, 999999).ToString();
            var otpToken = new OtpToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                OtpCode = otpCode,
                Purpose = 0, // ForgotPassword
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                CreatedAt = DateTime.UtcNow
            };

            await _otpTokenRepository.AddAsync(otpToken);
            await _emailService.SendOtpEmailAsync(user.Email, otpCode);
            return true;
        }

        public async Task<bool> VerifyOtpAsync(VerifyOtpInput input)
        {
            var user = await _userRepository.GetByEmailAsync(input.Email);
            if (user == null) return false;

            var otp = await _otpTokenRepository.GetValidOtpAsync(user.Id, input.OtpCode, 0);
            return otp != null;
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordInput input)
        {
            var user = await _userRepository.GetByEmailAsync(input.Email);
            if (user == null) return false;

            var otp = await _otpTokenRepository.GetValidOtpAsync(user.Id, input.OtpCode, 0);
            if (otp == null) return false;

            otp.IsUsed = true;
            await _otpTokenRepository.UpdateAsync(otp);

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(input.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            return true;
        }

        private AuthOutput GenerateAuthOutput(User user)
        {
            var jwtSettings = _config.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
            var expiresAt = DateTime.UtcNow.AddHours(24);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: expiresAt,
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

            return new AuthOutput
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role,
                ExpiresAt = expiresAt
            };
        }
    }
}
