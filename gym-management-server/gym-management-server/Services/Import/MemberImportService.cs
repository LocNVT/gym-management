using gym_management_server.Data.EntityFramework;
using gym_management_server.DTOs.Export;
using gym_management_server.Entities.Members;
using gym_management_server.Infrastructure.Excel;
using gym_management_server.Repositories.Members;
using gym_management_server.Services.Members;
using Microsoft.EntityFrameworkCore;

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
            catch (DbUpdateException)
            {
                // A duplicate check above already covers the common case; this remains for a
                // race with a concurrent import between our check and the commit. Report it the
                // same way as any other row problem rather than letting a raw 500 escape.
                await transaction.RollbackAsync();
                var conflictError = new RowError(1, null,
                    "Dữ liệu đã thay đổi trong khi xử lý file. Vui lòng kiểm tra lại và thử lại.");
                return new ImportResult(read.Rows.Count, 0, Committed: false, new List<RowError> { conflictError });
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

            // PhoneNumber carries an unfiltered unique index, so a soft-deleted member's number
            // still collides at the database even though it is invisible everywhere else in the
            // app. One query, tagged with each member's deleted state, so we can tell the two
            // cases apart without a second round trip.
            var existingByPhone = (await _members.GetAllPhoneNumbersWithDeletedStateAsync())
                .ToDictionary(x => x.PhoneNumber, x => x.IsDeleted, StringComparer.OrdinalIgnoreCase);
            var seenInFile = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < rows.Count; i++)
            {
                var phone = rows[i].PhoneNumber;
                var excelRow = i + 2;   // header is row 1

                if (existingByPhone.TryGetValue(phone, out var isDeleted))
                {
                    errors.Add(isDeleted
                        ? new RowError(excelRow, "Số điện thoại",
                            $"Số điện thoại {phone} thuộc về một hội viên đã bị xóa. Vui lòng khôi phục hội viên đó hoặc dùng số điện thoại khác.")
                        : new RowError(excelRow, "Số điện thoại",
                            $"Số điện thoại {phone} đã tồn tại trong hệ thống."));
                }

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
