using gym_management_server.Data.EntityFramework;
using gym_management_server.DTOs.Export;
using gym_management_server.Entities.ServicePackages;
using gym_management_server.Infrastructure.Excel;
using gym_management_server.Repositories.ServicePackages;
using gym_management_server.Services.ServicePackages;
using Microsoft.EntityFrameworkCore;

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
            errors.AddRange(await FindDuplicatesAsync(read.Rows, read.RowNumbers));

            if (errors.Count > 0 || dryRun)
                return new ImportResult(read.Rows.Count, 0, Committed: false, errors);

            await using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                await _packages.AddRangeAsync(read.Rows.Select(ToEntity));
                await transaction.CommitAsync();
            }
            catch (DbUpdateException ex)
            {
                // Symmetric with MemberImportService: a race with a concurrent import between
                // our duplicate check and the commit -- or any other write failure -- must not
                // escape as a raw 500, and must not be reported as a concurrency race unless it
                // actually was one.
                await transaction.RollbackAsync();
                var conflictError = ImportConflictMessage.For(ex);
                return new ImportResult(read.Rows.Count, 0, Committed: false, new List<RowError> { conflictError });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            return new ImportResult(read.Rows.Count, read.Rows.Count, Committed: true, errors);
        }

        private async Task<List<RowError>> FindDuplicatesAsync(IReadOnlyList<ServicePackageRow> rows, IReadOnlyList<int> rowNumbers)
        {
            var errors = new List<RowError>();
            var existing = (await _packages.GetAllNamesAsync())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var seenInFile = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < rows.Count; i++)
            {
                var name = rows[i].Name;
                // The reader skips blank rows silently (no entry in Rows, no error), so an
                // offset like "i + 2" drifts by one for every blank row earlier in the file.
                // RowNumbers carries each kept row's true physical Excel row instead.
                var excelRow = rowNumbers[i];

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
