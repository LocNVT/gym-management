using gym_management_server.DTOs.Common;
using gym_management_server.DTOs.Users;
using gym_management_server.Entities.Users;
using gym_management_server.Repositories.Users;

namespace gym_management_server.Services.Users
{
    /// <summary>
    /// Admin-only account management within the caller's own tenant (see
    /// docs/ImprovementPlan.md mục 1). Deliberately separate from AuthService, which only ever
    /// handles a user acting on their own account (login, register, password reset).
    /// </summary>
    public class UserManagementService
    {
        private readonly IUserRepository _userRepository;
        private readonly GymManagementServiceMapObjects _mapObjects;

        public UserManagementService(IUserRepository userRepository, GymManagementServiceMapObjects mapObjects)
        {
            _userRepository = userRepository;
            _mapObjects = mapObjects;
        }

        public async Task<PagedResult<UserOutput>> GetListAsync(int page = 1, int pageSize = 20)
        {
            var (items, totalCount) = await _userRepository.GetPagedAsync(page, pageSize);
            var mapped = items.Select(u => _mapObjects.MapObjects<User, UserOutput>(u)).ToList();
            return new PagedResult<UserOutput> { Items = mapped, TotalCount = totalCount, Page = page, PageSize = pageSize };
        }

        public async Task<UserOutput> CreateAsync(CreateUserInput input)
        {
            var existingUsername = await _userRepository.GetByUsernameAsync(input.Username);
            if (existingUsername != null)
                throw new InvalidOperationException("Username đã tồn tại.");

            var existingEmail = await _userRepository.GetByEmailAsync(input.Email);
            if (existingEmail != null)
                throw new InvalidOperationException("Email đã được sử dụng.");

            var user = new User
            {
                Id = Guid.NewGuid(),
                // No TenantId here: GymManagementContext.StampTenantId fills it in from the
                // calling Admin's own tenant - this account can only ever belong to that tenant.
                Username = input.Username,
                Email = input.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(input.Password),
                FullName = input.FullName,
                Role = input.Role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(user);
            return _mapObjects.MapObjects<User, UserOutput>(user);
        }

        public async Task<UserOutput?> SetActiveAsync(Guid id, bool isActive)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return null;

            user.IsActive = isActive;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
            return _mapObjects.MapObjects<User, UserOutput>(user);
        }

        public async Task<UserOutput?> SetRoleAsync(Guid id, byte role)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return null;

            user.Role = role;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
            return _mapObjects.MapObjects<User, UserOutput>(user);
        }
    }
}
