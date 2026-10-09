using gym_management_server.Data.EntityFramework;
using gym_management_server.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace gym_management_server.Services.CheckIns
{
    /// <summary>
    /// Closes attendance sessions nobody ever scanned/checked out of (see
    /// docs/ImprovementPlan.md mục 4) - without this, a member who forgets to scan out, or whose
    /// scan fails, stays "currently inside" forever and skews GetActiveAttendanceAsync and the
    /// attendance dashboard indefinitely.
    ///
    /// The threshold and poll interval are configurable because different gyms keep different
    /// hours (e.g. 24/7 vs. closing overnight). <see cref="CloseStaleSessionsAsync"/> is the
    /// testable core: it takes an explicit <see cref="GymManagementContext"/> and reads the clock
    /// from <see cref="TimeProvider"/>, so a test can simulate 24 hours passing without waiting for it.
    /// </summary>
    public class AutoCheckoutBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<AutoCheckoutBackgroundService> _logger;
        private readonly TimeSpan _pollInterval;
        private readonly TimeSpan _autoCheckoutThreshold;

        public AutoCheckoutBackgroundService(
            IServiceScopeFactory scopeFactory,
            TimeProvider timeProvider,
            IConfiguration configuration,
            ILogger<AutoCheckoutBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _timeProvider = timeProvider;
            _logger = logger;
            _pollInterval = TimeSpan.FromMinutes(configuration.GetValue("Attendance:AutoCheckoutPollMinutes", 15));
            _autoCheckoutThreshold = TimeSpan.FromHours(configuration.GetValue("Attendance:AutoCheckoutHours", 24));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(_pollInterval, _timeProvider);
            do
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<GymManagementContext>();
                    var closed = await CloseStaleSessionsAsync(db, stoppingToken);
                    if (closed > 0)
                        _logger.LogInformation("Auto check-out closed {Count} stale attendance session(s).", closed);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Auto check-out pass failed.");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }

        /// <summary>Closes every open session started before now minus the configured threshold.</summary>
        public async Task<int> CloseStaleSessionsAsync(GymManagementContext db, CancellationToken cancellationToken)
        {
            var cutoff = _timeProvider.GetUtcNow().UtcDateTime - _autoCheckoutThreshold;

            var stale = await db.CheckIns
                .Where(c => c.CheckOutTime == null && c.CheckInTime < cutoff)
                .ToListAsync(cancellationToken);

            foreach (var session in stale)
            {
                session.CheckOutTime = session.CheckInTime + _autoCheckoutThreshold;
                session.CheckOutMethod = CheckOutMethod.AutoTimeout;
            }

            if (stale.Count > 0)
                await db.SaveChangesAsync(cancellationToken);

            return stale.Count;
        }
    }
}
