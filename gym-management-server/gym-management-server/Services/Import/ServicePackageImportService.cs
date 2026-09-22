using gym_management_server.Data.EntityFramework;
using gym_management_server.DTOs.Export;
using gym_management_server.Entities.ServicePackages;
using gym_management_server.Infrastructure.Excel;
using gym_management_server.Repositories.ServicePackages;
using gym_management_server.Services.ServicePackages;

namespace gym_management_server.Services.Import
{
    public class ServicePackageImportService
    {
        private readonly IServicePackageRepository _packages;
        private readonly GymManagementContext _db;

        public ServicePackageImportService(IServicePackageRepository packages, GymManagementContext db)
        {
            _packages = packages;
            _db = db;
        }

        public async Task<ImportResult> ImportAsync(Stream file, bool dryRun)
        {
            var read = ExcelReader.Read(file, ServicePackageSheet.Import);
            var errors = read.Errors.ToList();

            // The reader cannot know about the database or about the rest of the file.
            errors.AddRange(await FindDuplicatesAsync(read.Rows));

            if (errors.Count > 0 || dryRun)
                return new ImportResult(read.Rows.Count, 0, Committed: false, errors);

            await using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                await _packages.AddRangeAsync(read.Rows.Select(ToEntity));
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            return new ImportResult(read.Rows.Count, read.Rows.Count, Committed: true, errors);
        }

        private async Task<List<RowError>> FindDuplicatesAsync(IReadOnlyList<ServicePackageRow> rows)
        {
            var errors = new List<RowError>();
            var existing = (await _packages.GetAllNamesAsync())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var seenInFile = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < rows.Count; i++)
            {
                var name = rows[i].Name;
                var excelRow = i + 2;   // header is row 1

                if (existing.Contains(name))
                    errors.Add(new RowError(excelRow, "Tên gói",
                        $"Tên gói \"{name}\" đã tồn tại trong hệ thống."));

                if (seenInFile.TryGetValue(name, out var firstRow))
                    errors.Add(new RowError(excelRow, "Tên gói",
                        $"Tên gói \"{name}\" bị trùng với dòng {firstRow} trong cùng file."));
                else
                    seenInFile[name] = excelRow;
            }

            return errors;
        }

        private static ServicePackage ToEntity(ServicePackageRow row) => new()
        {
            Id = Guid.NewGuid(),
            Name = row.Name,
            Description = row.Description,
            Price = row.Price,
            DurationDays = row.DurationDays,
            MaxCheckins = row.MaxCheckins,
            IsActive = row.IsActive,
            CreatedAt = DateTime.UtcNow,
        };
    }
}
