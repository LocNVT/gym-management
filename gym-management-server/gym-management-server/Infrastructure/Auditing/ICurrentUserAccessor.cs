namespace gym_management_server.Infrastructure.Auditing
{
    /// <summary>Who is making the current change, for the audit trail (see GymManagementContext).</summary>
    public interface ICurrentUserAccessor
    {
        Guid? UserId { get; }
        string? Username { get; }
    }
}
