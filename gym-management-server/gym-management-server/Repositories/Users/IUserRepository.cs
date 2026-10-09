using gym_management_server.Entities.Users;

namespace gym_management_server.Repositories.Users
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id);
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByGoogleIdAsync(string googleId);
        Task AddAsync(User user);
        Task UpdateAsync(User user);

        /// <summary>Every user in the caller's tenant (via the global query filter), for Admin's
        /// account management screen.</summary>
        Task<(List<User> Items, int TotalCount)> GetPagedAsync(int page, int pageSize);
    }
}
