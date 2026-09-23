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

        /// <summary>One point per month, oldest first, including months with no data as zeroes.</summary>
        Task<List<MonthPoint>> GetRevenueTrendAsync(int months);

        /// <summary>One point per month, oldest first, including months with no data as zeroes.</summary>
        Task<List<GrowthPoint>> GetMemberGrowthAsync(int months);

        Task<List<PackageSlice>> GetPackageDistributionAsync();

        /// <summary>Always 24 entries, hour 0..23, bucketed in local time.</summary>
        Task<List<HourSlice>> GetPeakHoursAsync(int days);

        /// <summary>Soonest-first.</summary>
        Task<List<ExpiringSoon>> GetExpiringSoonAsync(int days);
    }
}
