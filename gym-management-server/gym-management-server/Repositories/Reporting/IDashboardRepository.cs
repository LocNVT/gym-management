using gym_management_server.DTOs.Dashboard;

namespace gym_management_server.Repositories.Reporting
{
    /// <summary>
    /// Read-only aggregation for the dashboard. Deliberately separate from the CRUD
    /// repositories: they stay small and single-purpose, and nothing here writes.
    /// </summary>
    public interface IDashboardRepository
    {
        Task<KpiOutput> GetKpiAsync(int year, int month);
    }
}
