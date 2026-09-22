using gym_management_server.Data.EntityFramework;
using gym_management_server.DTOs.Export;
using gym_management_server.Entities.Members;
using gym_management_server.Infrastructure.Excel;
using gym_management_server.Repositories.Members;
using gym_management_server.Services.Members;

namespace gym_management_server.Services.Import
{
    public class MemberImportService
    {
        private readonly IMemberRepository _members;
        private readonly GymManagementContext _db;

        public MemberImportService(IMemberRepository members, GymManagementContext db)
        {
            _members = members;
            _db = db;
        }

        public async Task<ImportResult> ImportAsync(Stream file, bool dryRun)
        {
            var read = ExcelReader.Read(file, MemberSheet.Import);
            var errors = read.Errors.ToList();

            // The reader cannot know about the database or about the rest of the file.
            errors.AddRange(await FindDuplicatesAsync(read.Rows));

            if (errors.Count > 0 || dryRun)
                return new ImportResult(read.Rows.Count, 0, Committed: false, errors);

            await using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                await _members.AddRangeAsync(read.Rows.Select(ToEntity));
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            return new ImportResult(read.Rows.Count, read.Rows.Count, Committed: true, errors);
        }

        private async Task<List<RowError>> FindDuplicatesAsync(IReadOnlyList<MemberRow> rows)
        {
            var errors = new List<RowError>();
            var existing = (await _members.GetAllPhoneNumbersAsync())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var seenInFile = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < rows.Count; i++)
            {
                var phone = rows[i].PhoneNumber;
                var excelRow = i + 2;   // header is row 1

                if (existing.Contains(phone))
                    errors.Add(new RowError(excelRow, "Số điện thoại",
                        $"Số điện thoại {phone} đã tồn tại trong hệ thống."));

                if (seenInFile.TryGetValue(phone, out var firstRow))
                    errors.Add(new RowError(excelRow, "Số điện thoại",
                        $"Số điện thoại {phone} bị trùng với dòng {firstRow} trong cùng file."));
                else
                    seenInFile[phone] = excelRow;
            }

            return errors;
        }

        private static Member ToEntity(MemberRow row) => new()
        {
            Id = Guid.NewGuid(),
            FullName = row.FullName,
            PhoneNumber = row.PhoneNumber,
            Email = row.Email,
            DateOfBirth = row.DateOfBirth,
            Gender = row.Gender,
            Address = row.Address,
            EmergencyName = row.EmergencyName,
            EmergencyPhone = row.EmergencyPhone,
            Status = row.Status,
            Notes = row.Notes,
            RegistrationDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        };
    }
}
