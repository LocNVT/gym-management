using System.Threading.Tasks;

namespace GymManagement.Data;

public interface IGymManagementDbSchemaMigrator
{
    Task MigrateAsync();
}
