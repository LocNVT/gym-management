using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Threading.Tasks;
using gym_management_server.DTOs.Export;
using gym_management_server.DTOs.Fingerprints;
using gym_management_server.Entities.CheckIns;
using gym_management_server.Entities.Devices;
using gym_management_server.Entities.Members;
using gym_management_server.Fingerprints;
using gym_management_server.Fingerprints.Providers;
using gym_management_server.Repositories.CheckIns;
using gym_management_server.Repositories.Devices;
using gym_management_server.Repositories.Fingerprints;
using gym_management_server.Repositories.Members;
using gym_management_server.Services;
using gym_management_server.Services.Fingerprints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace gym_management_server.Tests.Integration
{
    /// <summary>
    /// Proves the fix for the check-in race condition described in docs/ImprovementPlan.md (mục 2):
    /// two near-simultaneous scans for the same member must never both see "no active session" and
    /// both insert a check-in. Needs a real database (SQLite here) because the EF Core InMemory
    /// provider does not enforce the unique filtered index that this fix relies on - see
    /// SqliteWebApplicationFactory's class comment.
    /// </summary>
    public class CheckInConcurrencyTests
    {
        /// <summary>
        /// Wraps the real repository but forces its FIRST GetActiveByMemberAsync call to report
        /// "no active session" regardless of DB state - simulating the stale read a request makes
        /// right before another, truly concurrent, request wins the race and inserts first.
        /// Every other call (including the service's post-conflict recheck) behaves normally.
        /// </summary>
        private class RaceSimulatingCheckInRepository : ICheckInRepository
        {
            private readonly ICheckInRepository _inner;
            private int _getActiveCalls;

            public RaceSimulatingCheckInRepository(ICheckInRepository inner) => _inner = inner;

            public Task<CheckIn?> GetActiveByMemberAsync(Guid memberId)
            {
                _getActiveCalls++;
                return _getActiveCalls == 1
                    ? Task.FromResult<CheckIn?>(null)
                    : _inner.GetActiveByMemberAsync(memberId);
            }

            public Task AddAsync(CheckIn checkIn) => _inner.AddAsync(checkIn);
            public Task UpdateAsync(CheckIn checkIn) => _inner.UpdateAsync(checkIn);
            public Task DeleteAsync(Guid id) => _inner.DeleteAsync(id);
            public Task<CheckIn?> GetByIdAsync(Guid id) => _inner.GetByIdAsync(id);
            public Task<List<CheckIn>> GetAllAsync(Expression<Func<CheckIn, bool>>? predicate = null) => _inner.GetAllAsync(predicate);
            public Task<(List<CheckIn> Items, int TotalCount)> GetPagedAsync(int page, int pageSize) => _inner.GetPagedAsync(page, pageSize);
            public Task<(List<CheckIn> Items, int TotalCount)> GetByMemberPagedAsync(Guid memberId, int page, int pageSize) => _inner.GetByMemberPagedAsync(memberId, page, pageSize);
            public Task<List<CheckIn>> GetActiveSessionsAsync() => _inner.GetActiveSessionsAsync();
            public Task<int> CountCheckInsBetweenAsync(DateTime from, DateTime to) => _inner.CountCheckInsBetweenAsync(from, to);
            public Task<int> CountCheckOutsBetweenAsync(DateTime from, DateTime to) => _inner.CountCheckOutsBetweenAsync(from, to);
            public Task<int> CountCurrentlyInsideAsync() => _inner.CountCurrentlyInsideAsync();
            public Task<List<CheckIn>> GetRecentAsync(int count) => _inner.GetRecentAsync(count);
            public Task<List<AttendanceRow>> GetForExportAsync(DateTime? from, DateTime? to) => _inner.GetForExportAsync(from, to);
        }

        private static string Template(string seed)
        {
            var bytes = new byte[32];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = (byte)((seed.GetHashCode() >> (i % 24)) ^ (seed[i % seed.Length] + i));
            return Convert.ToBase64String(bytes);
        }

        [Fact]
        public async Task Verify_does_not_double_check_in_when_the_active_session_read_was_stale()
        {
            using var fixture = new SqliteDbFixture();
            var db = fixture.NewContext();

            var member = new Member { Id = Guid.NewGuid(), FullName = "Race Member", PhoneNumber = "0999000111" };
            var device = new AttendanceDevice { Id = Guid.NewGuid(), Name = "Race Door", Vendor = "Mock", IsActive = true };
            db.Members.Add(member);
            db.AttendanceDevices.Add(device);
            db.SaveChanges();

            var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Fingerprint:EncryptionKey"] = key })
                .Build();
            var protector = new AesTemplateProtector(config);
            var factory = new FingerprintProviderFactory(new IFingerprintProvider[] { new MockFingerprintProvider() });
            var mapObjects = new GymManagementServiceMapObjects();

            var realCheckIns = new CheckInRepository(db);
            var racingCheckIns = new RaceSimulatingCheckInRepository(realCheckIns);

            var service = new FingerprintService(
                new FingerprintTemplateRepository(db),
                new AttendanceDeviceRepository(db),
                racingCheckIns,
                new MemberRepository(db),
                factory,
                protector,
                mapObjects,
                db.CurrentTenant);

            await service.RegisterAsync(new FingerprintTemplateInput
            {
                MemberId = member.Id,
                FingerPosition = 0,
                CapturedTemplate = Template("racer"),
                Vendor = "Mock"
            }, createdBy: null);

            // Simulates another, truly concurrent request that already opened a session for this
            // member - inserted directly, bypassing the service, exactly as a second thread's
            // successful insert would have landed between this request's read and its own insert.
            db.CheckIns.Add(new CheckIn
            {
                Id = Guid.NewGuid(),
                MemberId = member.Id,
                CheckInTime = DateTime.UtcNow,
                DeviceId = device.Id
            });
            db.SaveChanges();

            var result = await service.VerifyAsync(new VerifyFingerprintInput
            {
                DeviceId = device.Id,
                CapturedTemplate = Template("racer")
            });

            // The stale read said "no active session", but the unique index rejected the insert;
            // the service must recover by closing the session that was really there, not crash
            // and not create a second open session.
            Assert.True(result.Matched);
            Assert.Equal("check-out", result.Action);

            Assert.Equal(1, await db.CheckIns.CountAsync());
            var session = await db.CheckIns.SingleAsync();
            Assert.NotNull(session.CheckOutTime);
        }
    }
}
