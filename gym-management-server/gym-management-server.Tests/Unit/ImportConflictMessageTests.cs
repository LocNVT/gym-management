using gym_management_server.Services.Import;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace gym_management_server.Tests.Unit
{
    /// <summary>
    /// Final whole-branch review, item 2: the DbUpdateException catch in both import services
    /// used to always claim "dữ liệu đã thay đổi" (a concurrency race), even for a failure that
    /// plainly wasn't one (e.g. a truncation error surfacing there because a length check was
    /// missing upstream). SqlException itself has no accessible public constructor, so the
    /// duplicate-key and truncation branches (which inspect ex.InnerException as SqlException)
    /// are exercised at the integration level instead (ImportRollbackTests, via the SQLite-backed
    /// test doubles) or would need SQL Server itself. This covers the branch that IS reachable
    /// with a plain DbUpdateException: no recognizable inner exception at all, which must not be
    /// asserted as a concurrency conflict either.
    /// </summary>
    public class ImportConflictMessageTests
    {
        [Fact]
        public void A_DbUpdateException_with_no_recognizable_inner_exception_gets_an_honest_uncertain_message()
        {
            var error = ImportConflictMessage.For(new DbUpdateException("boom, cause unknown"));

            Assert.False(string.IsNullOrWhiteSpace(error.Message));
            // Must NOT assert the concurrency-race story for a cause it cannot verify.
            Assert.DoesNotContain("đã thay đổi", error.Message);
            Assert.DoesNotContain("người khác nhập", error.Message);
        }

        [Fact]
        public void The_uncertain_message_does_not_claim_a_truncation_cause_either()
        {
            var error = ImportConflictMessage.For(new DbUpdateException("boom, cause unknown"));
            Assert.DoesNotContain("vượt quá độ dài", error.Message);
        }
    }
}
