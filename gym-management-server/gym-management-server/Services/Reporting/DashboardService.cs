using gym_management_server.DTOs.Dashboard;
using gym_management_server.Infrastructure.Time;
using gym_management_server.Repositories.Reporting;

namespace gym_management_server.Services.Reporting
{
    public class DashboardService
    {
        private readonly IDashboardRepository _repository;
        public DashboardService(IDashboardRepository repository) => _repository = repository;

        public async Task<KpiOutput> GetKpiAsync(int? year, int? month, bool includeFinancials)
        {
            var now = GymClock.LocalNow;
            var kpi = await _repository.GetKpiAsync(year ?? now.Year, month ?? now.Month);

            // Staff keep the operational half of the page; only the money is withheld.
            return includeFinancials
                ? kpi
                : kpi with
                {
                    Revenue = null,
                    PreviousRevenue = null,
                    Expense = null,
                    PreviousExpense = null,
                    Profit = null,
                    UnpaidTotal = null,
                };
        }

        public Task<List<MonthPoint>> GetRevenueTrendAsync(int months) =>
            _repository.GetRevenueTrendAsync(Clamp(months, 1, 36));

        public Task<List<GrowthPoint>> GetMemberGrowthAsync(int months) =>
            _repository.GetMemberGrowthAsync(Clamp(months, 1, 36));

        public Task<List<PackageSlice>> GetPackageDistributionAsync() =>
            _repository.GetPackageDistributionAsync();

        public Task<List<HourSlice>> GetPeakHoursAsync(int days) =>
            _repository.GetPeakHoursAsync(Clamp(days, 1, 365));

        public Task<List<ExpiringSoon>> GetExpiringSoonAsync(int days) =>
            _repository.GetExpiringSoonAsync(Clamp(days, 1, 365));

        private static int Clamp(int value, int min, int max) => Math.Clamp(value, min, max);
    }
}
