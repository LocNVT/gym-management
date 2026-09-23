using gym_management_server.Data.EntityFramework;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace gym_management_server.Tests.Integration
{
    /// <summary>
    /// A real SQL database for repository tests. The EF InMemory provider evaluates LINQ in
    /// memory, so a GroupBy that cannot be translated to SQL still passes there and then
    /// falls over on SQL Server. SQLite catches that.
    /// </summary>
    public sealed class SqliteDbFixture : IDisposable
    {
        private readonly List<SqliteConnection> _connections = new();

        public GymManagementContext NewContext()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();
            _connections.Add(connection);

            var context = new GymManagementContext(
                new DbContextOptionsBuilder<GymManagementContext>().UseSqlite(connection).Options);
            context.Database.EnsureCreated();
            return context;
        }

        public void Dispose()
        {
            foreach (var connection in _connections) connection.Dispose();
        }
    }
}
