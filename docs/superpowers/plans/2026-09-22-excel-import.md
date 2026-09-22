# Excel Import Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let staff bulk-load members and service packages from a spreadsheet, with a dry run that lists every bad row before anything is written, and a commit that is all-or-nothing.

**Architecture:** An `ExcelReader` walks a `SheetDefinition<T>`'s columns and fills a row object, collecting a `RowError` per problem instead of throwing on the first one. The import service adds the checks a parser cannot do alone — duplicates against the database and duplicates *within the file*. Nothing is written unless the row list is completely clean, and the write happens inside one transaction. There is no server-side state between the dry run and the commit: the confirm step re-sends the same file.

**Tech Stack:** ASP.NET Core 8, EF Core 9, ClosedXML, xUnit, EF Core SQLite (tests only), Angular 20, Angular Material dialog.

**Spec:** `docs/superpowers/specs/2026-09-22-excel-dashboard-design.md` (section 6)

**Depends on:** `docs/superpowers/plans/2026-09-22-enum-and-excel-export.md` must be complete. This plan consumes `ExcelColumn<T>`, `SheetDefinition<T>`, `MemberRow`, `ServicePackageRow`, `MemberSheet.Import`, `ServicePackageSheet.Import`, `EnumLabel.TryParseLabel` and `CustomWebApplicationFactory.CreateAuthenticatedClient` from it.

## Global Constraints

- Target framework is `net8.0`. Run tests with `dotnet test` from `gym-management-server/`.
- Only two modules are importable: **Member** and **ServicePackage**. Do not add a third.
- Import is **all-or-nothing**. One bad row means zero rows written.
- Error messages are Vietnamese and quote the row number the user sees in Excel (the header is row 1, so the first data row is row 2).
- Import endpoints carry `[Authorize]`.
- Uploads are capped at **5 MB** and **10,000 data rows**.
- Repositories return materialised `List<T>`; do not leak `IQueryable`.

## A Testing Trap, Read Before Task 4

The existing `CustomWebApplicationFactory` uses the EF Core **InMemory** provider. It silently does the wrong thing for this feature in two ways:

1. **It ignores transactions.** `BeginTransactionAsync` raises `TransactionIgnoredWarning`; suppress the warning and the rollback still does not happen. A rollback test on InMemory passes while proving nothing.
2. **It does not enforce unique indexes.** The unique index on `Members.PhoneNumber` is not applied, so a duplicate-phone test would pass even with the check removed.

Task 4 therefore introduces a **SQLite in-memory** fixture for the import tests. SQLite honours both transactions and unique indexes. Do not "simplify" it back to the InMemory provider.

---

### Task 1: The Excel reader

**Files:**
- Create: `Infrastructure/Excel/RowError.cs`, `Infrastructure/Excel/ReadResult.cs`, `Infrastructure/Excel/ExcelReader.cs`
- Test: `gym-management-server.Tests/Unit/ExcelReaderTests.cs`

**Interfaces:**
- Consumes: `ExcelColumn<T>`, `SheetDefinition<T>` from the export plan.
- Produces, in `gym_management_server.Infrastructure.Excel`:
  - `record RowError(int RowNumber, string? ColumnHeader, string Message)` — `RowNumber` is the Excel row the user sees; `ColumnHeader` is null for whole-row or whole-file problems.
  - `sealed class ReadResult<T> { IReadOnlyList<T> Rows; IReadOnlyList<RowError> Errors; bool IsClean => Errors.Count == 0; }`
  - `static class ExcelReader` with `const int MaxRows = 10_000` and `ReadResult<T> Read<T>(Stream stream, SheetDefinition<T> sheet) where T : new()`

**Reader rules:**
- The header row is matched **by name, not position**, so a user may reorder or delete columns. Matching trims and ignores case.
- A column in the sheet definition with `Parse == null` is skipped even if present in the file. This is what lets an exported file be re-imported without deleting its ID and timestamp columns.
- A required column missing from the header is one file-level error (`RowNumber = 1`), not one error per row.
- A completely blank row is skipped, not reported.
- Every problem is collected; reading never stops at the first one.

- [ ] **Step 1: Write the failing test**

Create `gym-management-server.Tests/Unit/ExcelReaderTests.cs`:

```csharp
using System.IO;
using ClosedXML.Excel;
using gym_management_server.Infrastructure.Excel;
using Xunit;

namespace gym_management_server.Tests.Unit
{
    public class ExcelReaderTests
    {
        private sealed class Row
        {
            public string Name { get; set; } = "";
            public int Age { get; set; }
            public Guid Id { get; set; }   // read-only column
        }

        private static SheetDefinition<Row> Sheet => new("Dữ liệu", new[]
        {
            new ExcelColumn<Row>("Tên", r => r.Name, isRequired: true,
                parse: (r, v) => r.Name = v.Trim()),
            new ExcelColumn<Row>("Tuổi", r => r.Age,
                parse: (r, v) => r.Age = int.TryParse(v, out var n)
                    ? n
                    : throw new FormatException("Tuổi phải là số nguyên.")),
            new ExcelColumn<Row>("Mã", r => r.Id),   // no parse -> read-only
        });

        private static Stream Build(params string[][] rows)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Dữ liệu");
            for (var r = 0; r < rows.Length; r++)
                for (var c = 0; c < rows[r].Length; c++)
                    ws.Cell(r + 1, c + 1).Value = rows[r][c];

            var stream = new MemoryStream();
            wb.SaveAs(stream);
            stream.Position = 0;
            return stream;
        }

        [Fact]
        public void Reads_clean_rows()
        {
            var result = ExcelReader.Read(
                Build(["Tên", "Tuổi"], ["Nguyễn Văn A", "30"], ["Trần Thị B", "25"]), Sheet);

            Assert.True(result.IsClean);
            Assert.Equal(2, result.Rows.Count);
            Assert.Equal("Nguyễn Văn A", result.Rows[0].Name);
            Assert.Equal(25, result.Rows[1].Age);
        }

        [Fact]
        public void Reports_the_excel_row_number_the_user_sees()
        {
            // Header is row 1, so the second data row is Excel row 3.
            var result = ExcelReader.Read(
                Build(["Tên", "Tuổi"], ["Hợp lệ", "30"], ["Sai", "ba mươi"]), Sheet);

            var error = Assert.Single(result.Errors);
            Assert.Equal(3, error.RowNumber);
            Assert.Equal("Tuổi", error.ColumnHeader);
            Assert.Contains("số nguyên", error.Message);
        }

        [Fact]
        public void Collects_every_error_rather_than_stopping_at_the_first()
        {
            var result = ExcelReader.Read(
                Build(["Tên", "Tuổi"], ["", "x"], ["", "y"]), Sheet);

            Assert.Equal(4, result.Errors.Count);   // 2 rows x (missing name + bad age)
        }

        [Fact]
        public void A_missing_required_column_is_one_file_level_error()
        {
            var result = ExcelReader.Read(Build(["Tuổi"], ["30"], ["40"]), Sheet);

            var error = Assert.Single(result.Errors);
            Assert.Equal(1, error.RowNumber);
            Assert.Contains("Tên", error.Message);
        }

        [Fact]
        public void Columns_may_be_reordered_or_omitted()
        {
            var result = ExcelReader.Read(Build(["Tuổi", "Tên"], ["30", "Nguyễn Văn A"]), Sheet);

            Assert.True(result.IsClean);
            Assert.Equal("Nguyễn Văn A", result.Rows[0].Name);
            Assert.Equal(30, result.Rows[0].Age);
        }

        [Fact]
        public void Read_only_columns_present_in_the_file_are_ignored_not_rejected()
        {
            // This is what lets a freshly exported file be edited and re-imported as-is.
            var result = ExcelReader.Read(
                Build(["Tên", "Tuổi", "Mã"], ["Nguyễn Văn A", "30", "không-phải-guid"]), Sheet);

            Assert.True(result.IsClean);
            Assert.Equal(Guid.Empty, result.Rows[0].Id);
        }

        [Fact]
        public void Blank_rows_are_skipped_silently()
        {
            var result = ExcelReader.Read(
                Build(["Tên", "Tuổi"], ["Nguyễn Văn A", "30"], ["", ""], ["Trần Thị B", "25"]), Sheet);

            Assert.True(result.IsClean);
            Assert.Equal(2, result.Rows.Count);
        }

        [Fact]
        public void A_file_over_the_row_limit_is_one_file_level_error()
        {
            var rows = new List<string[]> { ["Tên", "Tuổi"] };
            for (var i = 0; i <= ExcelReader.MaxRows; i++) rows.Add([$"HV{i}", "30"]);

            var result = ExcelReader.Read(Build(rows.ToArray()), Sheet);

            Assert.Contains(result.Errors, e => e.Message.Contains("vượt giới hạn"));
            Assert.Empty(result.Rows);
        }
    }
}
```

- [ ] **Step 2: Run the test and confirm it fails**

```bash
dotnet test gym-management-server/gym-management-server.sln --filter FullyQualifiedName~ExcelReaderTests
```

Expected: compile error — `ExcelReader` does not exist.

- [ ] **Step 3: Write the result types**

Create `Infrastructure/Excel/RowError.cs`:

```csharp
namespace gym_management_server.Infrastructure.Excel
{
    /// <summary>
    /// One problem found in an uploaded file. <paramref name="RowNumber"/> is the row number
    /// as Excel shows it to the user (header = 1), so they can go straight to the cell.
    /// A null <paramref name="ColumnHeader"/> means the problem is with the row or the file
    /// as a whole rather than one cell.
    /// </summary>
    public record RowError(int RowNumber, string? ColumnHeader, string Message);
}
```

Create `Infrastructure/Excel/ReadResult.cs`:

```csharp
namespace gym_management_server.Infrastructure.Excel
{
    public sealed class ReadResult<T>
    {
        public ReadResult(IReadOnlyList<T> rows, IReadOnlyList<RowError> errors)
        {
            Rows = rows;
            Errors = errors;
        }

        public IReadOnlyList<T> Rows { get; }
        public IReadOnlyList<RowError> Errors { get; }
        public bool IsClean => Errors.Count == 0;
    }
}
```

- [ ] **Step 4: Write the reader**

Create `Infrastructure/Excel/ExcelReader.cs`:

```csharp
using ClosedXML.Excel;

namespace gym_management_server.Infrastructure.Excel
{
    public static class ExcelReader
    {
        public const int MaxRows = 10_000;

        public static ReadResult<T> Read<T>(Stream stream, SheetDefinition<T> sheet) where T : new()
        {
            var errors = new List<RowError>();
            var rows = new List<T>();

            using var workbook = new XLWorkbook(stream);
            var ws = workbook.Worksheets.FirstOrDefault(w => w.Name == sheet.SheetName)
                     ?? workbook.Worksheets.First();

            // Only columns with a Parse can be filled from the file; the rest are export-only.
            var writable = sheet.Columns.Where(c => c.Parse is not null).ToList();

            // Match the header by name, so the user may reorder or drop columns.
            var headerRow = ws.Row(1);
            var positions = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var cell in headerRow.CellsUsed())
            {
                var name = cell.GetString().Trim();
                if (name.Length > 0) positions.TryAdd(name, cell.Address.ColumnNumber);
            }

            foreach (var column in writable.Where(c => c.IsRequired && !positions.ContainsKey(c.Header)))
                errors.Add(new RowError(1, column.Header, $"Thiếu cột bắt buộc \"{column.Header}\"."));

            if (errors.Count > 0) return new ReadResult<T>(rows, errors);

            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
            if (lastRow - 1 > MaxRows)
            {
                errors.Add(new RowError(1, null,
                    $"File có {lastRow - 1:N0} dòng, vượt giới hạn {MaxRows:N0} dòng mỗi lần nhập."));
                return new ReadResult<T>(rows, errors);
            }

            for (var r = 2; r <= lastRow; r++)
            {
                var excelRow = ws.Row(r);
                if (excelRow.IsEmpty()) continue;

                var item = new T();
                var rowHadError = false;

                foreach (var column in writable)
                {
                    if (!positions.TryGetValue(column.Header, out var columnNumber)) continue;

                    var raw = ws.Cell(r, columnNumber).GetFormattedString().Trim();

                    if (raw.Length == 0)
                    {
                        if (column.IsRequired)
                        {
                            errors.Add(new RowError(r, column.Header, $"\"{column.Header}\" không được để trống."));
                            rowHadError = true;
                        }
                        continue;
                    }

                    try
                    {
                        column.Parse!(item, raw);
                    }
                    catch (Exception ex)
                    {
                        errors.Add(new RowError(r, column.Header, ex.Message));
                        rowHadError = true;
                    }
                }

                if (!rowHadError) rows.Add(item);
            }

            return new ReadResult<T>(rows, errors);
        }
    }
}
```

- [ ] **Step 5: Run the tests and confirm they pass**

```bash
dotnet test gym-management-server/gym-management-server.sln --filter FullyQualifiedName~ExcelReaderTests
```

Expected: 8 passing.

- [ ] **Step 6: Commit**

```bash
git add gym-management-server/
git commit -m "Add an Excel reader that collects every row error instead of throwing"
```

---

### Task 2: Parse handlers and the template writer

**Files:**
- Modify: `Services/Members/MemberSheet.cs`, `Services/ServicePackages/ServicePackageSheet.cs`
- Create: `Infrastructure/Excel/ExcelTemplateWriter.cs`
- Test: `gym-management-server.Tests/Unit/ImportSheetTests.cs`, `gym-management-server.Tests/Unit/ExcelTemplateWriterTests.cs`

**Interfaces:**
- Consumes: `EnumLabel.TryParseLabel`, `MemberSheet.Import`, `ServicePackageSheet.Import`.
- Produces: every column in `MemberSheet.Import` and `ServicePackageSheet.Import` gains a `parse:` handler. `ExcelTemplateWriter.Write<T>(SheetDefinition<T> sheet) -> byte[]` emits the header row, one example row, and a dropdown restricting any column with `AllowedValues`.

**Parsing rules:**
- Dates accept `dd/MM/yyyy` and `d/M/yyyy`, Vietnamese order. Reject `MM/dd/yyyy` ambiguity by parsing with an explicit format list and `CultureInfo.InvariantCulture`.
- Numbers tolerate thousands separators (`1.500.000` and `1,500,000`) because that is what a real spreadsheet contains.
- Enum cells accept the Vietnamese label or the English member name, any casing.
- Booleans accept `Có`/`Không`, `true`/`false`, `1`/`0`.
- A parse failure throws with a Vietnamese message; `ExcelReader` turns it into a `RowError`.

- [ ] **Step 1: Write the failing test**

Create `gym-management-server.Tests/Unit/ImportSheetTests.cs`:

```csharp
using gym_management_server.DTOs.Export;
using gym_management_server.Entities.Enums;
using gym_management_server.Services.Members;
using gym_management_server.Services.ServicePackages;
using Xunit;

namespace gym_management_server.Tests.Unit
{
    public class ImportSheetTests
    {
        private static void Parse(string header, MemberRow row, string value) =>
            MemberSheet.Import.Columns.Single(c => c.Header == header).Parse!(row, value);

        [Fact]
        public void Every_import_column_can_be_parsed()
        {
            Assert.All(MemberSheet.Import.Columns, c => Assert.NotNull(c.Parse));
            Assert.All(ServicePackageSheet.Import.Columns, c => Assert.NotNull(c.Parse));
        }

        [Fact]
        public void Dates_are_read_in_Vietnamese_day_first_order()
        {
            var row = new MemberRow();
            Parse("Ngày sinh", row, "09/03/1995");
            Assert.Equal(new DateTime(1995, 3, 9), row.DateOfBirth);
        }

        [Fact]
        public void An_unparseable_date_explains_the_expected_format()
        {
            var ex = Assert.Throws<FormatException>(() => Parse("Ngày sinh", new MemberRow(), "1995-03-09x"));
            Assert.Contains("dd/MM/yyyy", ex.Message);
        }

        [Fact]
        public void Enum_cells_accept_the_Vietnamese_label_in_any_casing()
        {
            var row = new MemberRow();
            Parse("Trạng thái", row, "  tạm ngưng ");
            Assert.Equal(MemberStatus.Suspended, row.Status);

            Parse("Giới tính", row, "Nữ");
            Assert.Equal(Gender.Female, row.Gender);
        }

        [Fact]
        public void An_unknown_enum_cell_lists_the_accepted_values()
        {
            var ex = Assert.Throws<FormatException>(() => Parse("Trạng thái", new MemberRow(), "Đang nghỉ"));
            Assert.Contains("Hoạt động", ex.Message);
            Assert.Contains("Tạm ngưng", ex.Message);
        }

        [Theory]
        [InlineData("1.500.000", 1500000)]
        [InlineData("1,500,000", 1500000)]
        [InlineData("1500000", 1500000)]
        public void Prices_tolerate_the_thousands_separators_real_spreadsheets_contain(string input, decimal expected)
        {
            var row = new ServicePackageRow();
            ServicePackageSheet.Import.Columns.Single(c => c.Header == "Đơn giá").Parse!(row, input);
            Assert.Equal(expected, row.Price);
        }

        [Theory]
        [InlineData("Có", true)]
        [InlineData("không", false)]
        [InlineData("1", true)]
        [InlineData("false", false)]
        public void Booleans_accept_the_Vietnamese_words_and_the_usual_literals(string input, bool expected)
        {
            var row = new ServicePackageRow();
            ServicePackageSheet.Import.Columns.Single(c => c.Header == "Đang áp dụng").Parse!(row, input);
            Assert.Equal(expected, row.IsActive);
        }
    }
}
```

Create `gym-management-server.Tests/Unit/ExcelTemplateWriterTests.cs`:

```csharp
using System.IO;
using ClosedXML.Excel;
using gym_management_server.Infrastructure.Excel;
using gym_management_server.Services.Members;
using Xunit;

namespace gym_management_server.Tests.Unit
{
    public class ExcelTemplateWriterTests
    {
        private static IXLWorksheet Template()
        {
            var bytes = ExcelTemplateWriter.Write(MemberSheet.Import);
            return new XLWorkbook(new MemoryStream(bytes)).Worksheet("Thành viên");
        }

        [Fact]
        public void Template_has_the_import_headers_and_nothing_export_only()
        {
            var ws = Template();
            Assert.Equal("Họ và tên", ws.Cell(1, 1).GetString());
            Assert.Equal(MemberSheet.Import.Columns.Count, ws.Row(1).CellsUsed().Count());
        }

        [Fact]
        public void Template_includes_one_example_row_so_the_expected_format_is_obvious()
        {
            var ws = Template();
            Assert.False(ws.Cell(2, 1).IsEmpty());
        }

        [Fact]
        public void Enum_columns_get_a_dropdown_listing_the_allowed_values()
        {
            var ws = Template();
            var statusIndex = MemberSheet.Import.Columns
                .Select((c, i) => (c, i)).Single(x => x.c.Header == "Trạng thái").i + 1;

            var validation = ws.Cell(3, statusIndex).GetDataValidation();
            Assert.NotNull(validation);
            Assert.Contains("Hoạt động", validation!.Value);
        }
    }
}
```

- [ ] **Step 2: Run the tests and confirm they fail**

```bash
dotnet test gym-management-server/gym-management-server.sln --filter "FullyQualifiedName~ImportSheetTests|FullyQualifiedName~ExcelTemplateWriterTests"
```

Expected: the sheet tests fail with `NullReferenceException` on `Parse!`; the template tests fail to compile.

- [ ] **Step 3: Write the shared cell parsers**

Create `Infrastructure/Excel/CellParse.cs`:

```csharp
using System.Globalization;
using gym_management_server.Infrastructure.Enums;

namespace gym_management_server.Infrastructure.Excel
{
    /// <summary>
    /// Cell-to-value conversions shared by every import sheet. Each throws a
    /// FormatException whose message is shown to the user verbatim, so the messages
    /// are Vietnamese and say what was expected.
    /// </summary>
    public static class CellParse
    {
        private static readonly string[] DateFormats =
            ["dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy", "yyyy-MM-dd"];

        public static DateTime Date(string raw)
        {
            if (DateTime.TryParseExact(raw, DateFormats, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var value))
                return value;
            throw new FormatException($"\"{raw}\" không phải ngày hợp lệ. Định dạng mong đợi: dd/MM/yyyy.");
        }

        public static decimal Money(string raw)
        {
            // A real spreadsheet contains "1.500.000" or "1,500,000"; both mean the same thing.
            var digits = raw.Replace(".", "").Replace(",", "").Replace(" ", "");
            if (decimal.TryParse(digits, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
                return value;
            throw new FormatException($"\"{raw}\" không phải số tiền hợp lệ.");
        }

        public static int Integer(string raw)
        {
            var digits = raw.Replace(".", "").Replace(",", "").Replace(" ", "");
            if (int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                return value;
            throw new FormatException($"\"{raw}\" phải là số nguyên.");
        }

        public static bool Boolean(string raw) => raw.Trim().ToLowerInvariant() switch
        {
            "có" or "co" or "true" or "1" or "x" => true,
            "không" or "khong" or "false" or "0" or "" => false,
            _ => throw new FormatException($"\"{raw}\" phải là Có hoặc Không."),
        };

        public static TEnum Enum<TEnum>(string raw) where TEnum : struct, Enum
        {
            if (EnumLabel.TryParseLabel<TEnum>(raw, out var value)) return value;

            var allowed = string.Join(", ", EnumLabel.Describe<TEnum>().Select(v => v.Label));
            throw new FormatException($"\"{raw}\" không hợp lệ. Giá trị cho phép: {allowed}.");
        }

        public static string Phone(string raw)
        {
            var digits = new string(raw.Where(char.IsDigit).ToArray());
            if (digits.Length is < 9 or > 11)
                throw new FormatException($"\"{raw}\" không phải số điện thoại hợp lệ.");
            return digits;
        }

        public static string Email(string raw)
        {
            if (!raw.Contains('@') || raw.StartsWith('@') || raw.EndsWith('@'))
                throw new FormatException($"\"{raw}\" không phải email hợp lệ.");
            return raw;
        }
    }
}
```

- [ ] **Step 4: Add the parse handlers to the two import sheets**

In `Services/Members/MemberSheet.cs`, add `parse:` to every column of `Import`:

```csharp
public static SheetDefinition<MemberRow> Import => new("Thành viên", new[]
{
    new ExcelColumn<MemberRow>("Họ và tên", r => r.FullName, isRequired: true, width: 28,
        parse: (r, v) => r.FullName = v),
    new ExcelColumn<MemberRow>("Số điện thoại", r => r.PhoneNumber, isRequired: true,
        parse: (r, v) => r.PhoneNumber = CellParse.Phone(v)),
    new ExcelColumn<MemberRow>("Email", r => r.Email, width: 26,
        parse: (r, v) => r.Email = CellParse.Email(v)),
    new ExcelColumn<MemberRow>("Ngày sinh", r => r.DateOfBirth, format: "dd/MM/yyyy",
        parse: (r, v) => r.DateOfBirth = CellParse.Date(v)),
    new ExcelColumn<MemberRow>("Giới tính", r => r.Gender.ToLabel(), allowedValues: Labels<Gender>(),
        parse: (r, v) => r.Gender = CellParse.Enum<Gender>(v)),
    new ExcelColumn<MemberRow>("Địa chỉ", r => r.Address, width: 34,
        parse: (r, v) => r.Address = v),
    new ExcelColumn<MemberRow>("Người liên hệ khẩn cấp", r => r.EmergencyName, width: 24,
        parse: (r, v) => r.EmergencyName = v),
    new ExcelColumn<MemberRow>("SĐT khẩn cấp", r => r.EmergencyPhone,
        parse: (r, v) => r.EmergencyPhone = CellParse.Phone(v)),
    new ExcelColumn<MemberRow>("Trạng thái", r => r.Status.ToLabel(), allowedValues: Labels<MemberStatus>(),
        parse: (r, v) => r.Status = CellParse.Enum<MemberStatus>(v)),
    new ExcelColumn<MemberRow>("Ghi chú", r => r.Notes, width: 34,
        parse: (r, v) => r.Notes = v),
});
```

Do the same for `ServicePackageSheet.Import`: `Tên gói` → `r.Name = v`, `Mô tả` → `r.Description = v`, `Đơn giá` → `CellParse.Money`, `Số ngày` → `CellParse.Integer`, `Số lượt tối đa` → `CellParse.Integer`, `Đang áp dụng` → `CellParse.Boolean`.

- [ ] **Step 5: Write the template writer**

Create `Infrastructure/Excel/ExcelTemplateWriter.cs`:

```csharp
using ClosedXML.Excel;

namespace gym_management_server.Infrastructure.Excel
{
    public static class ExcelTemplateWriter
    {
        private const int ValidatedRows = 500;

        public static byte[] Write<T>(SheetDefinition<T> sheet)
        {
            var columns = sheet.Columns.Where(c => c.Parse is not null).ToList();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add(sheet.SheetName);

            for (var c = 0; c < columns.Count; c++)
            {
                var column = columns[c];
                var header = ws.Cell(1, c + 1);
                header.Value = column.Header + (column.IsRequired ? " *" : "");
                header.Style.Font.Bold = true;
                header.Style.Fill.BackgroundColor =
                    column.IsRequired ? XLColor.LightSalmon : XLColor.LightGray;
                ws.Column(c + 1).Width = column.Width;

                // One example row, so the expected format is obvious without reading a manual.
                ws.Cell(2, c + 1).Value = Example(column);

                if (column.AllowedValues is { Count: > 0 } allowed)
                    ws.Range(2, c + 1, ValidatedRows, c + 1)
                      .CreateDataValidation()
                      .List(string.Join(",", allowed), inCellDropdown: true);
            }

            ws.Row(2).Style.Font.Italic = true;
            ws.Row(2).Style.Font.FontColor = XLColor.Gray;
            ws.SheetView.FreezeRows(1);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private static string Example<T>(ExcelColumn<T> column)
        {
            if (column.AllowedValues is { Count: > 0 } allowed) return allowed[0];
            if (column.Format is not null && column.Format.Contains('/')) return "01/01/2026";
            if (column.Format is not null && column.Format.Contains('#')) return "1000000";
            return $"(ví dụ {column.Header.ToLowerInvariant()})";
        }
    }
}
```

**Header note:** the writer appends ` *` to required headers for the user's benefit, but `ExcelReader` matches on the bare header. Add the trim in the reader's header scan — in `ExcelReader.Read`, change the header capture to strip a trailing asterisk:

```csharp
var name = cell.GetString().Trim().TrimEnd('*').Trim();
```

- [ ] **Step 6: Run the tests and confirm they pass**

```bash
dotnet test gym-management-server/gym-management-server.sln --filter "FullyQualifiedName~ImportSheetTests|FullyQualifiedName~ExcelTemplateWriterTests|FullyQualifiedName~ExcelReaderTests"
```

Expected: all passing, including the reader tests still green after the asterisk change.

- [ ] **Step 7: Commit**

```bash
git add gym-management-server/
git commit -m "Add cell parsers, import parse handlers and the template writer"
```

---

### Task 3: The import service

**Files:**
- Create: `Services/Import/ImportResult.cs`, `Services/Import/MemberImportService.cs`, `Services/Import/ServicePackageImportService.cs`
- Modify: `Repositories/Members/IMemberRepository.cs.cs` + `MemberRepository.cs`, `Repositories/ServicePackages/IServicePackageRepository.cs` + `ServicePackageRepository.cs`
- Modify: `Program.cs` (register the two services)
- Test: covered by Task 4's fixture-based tests

**Interfaces:**
- Consumes: `ExcelReader.Read`, `MemberSheet.Import`, `ServicePackageSheet.Import`.
- Produces:
  - `record ImportResult(int TotalRows, int ImportedRows, bool Committed, IReadOnlyList<RowError> Errors) { bool IsClean => Errors.Count == 0; }`
  - `MemberImportService.Task<ImportResult> ImportAsync(Stream file, bool dryRun)`
  - `ServicePackageImportService.Task<ImportResult> ImportAsync(Stream file, bool dryRun)`
  - `IMemberRepository.Task<List<string>> GetAllPhoneNumbersAsync()` and `Task AddRangeAsync(IEnumerable<Member> members)`
  - `IServicePackageRepository.Task<List<string>> GetAllNamesAsync()` and `Task AddRangeAsync(IEnumerable<ServicePackage> packages)`

**Duplicate checks, both required:**
1. Against the database — a phone number (or package name) that already exists.
2. Within the file itself — two rows sharing a phone number. Easy to forget, and without it the transaction fails on the unique index with an error the user cannot act on.

- [ ] **Step 1: Add the repository methods**

In `MemberRepository`:

```csharp
public async Task<List<string>> GetAllPhoneNumbersAsync() =>
    await _db.Members.Where(x => !x.IsDeleted).Select(x => x.PhoneNumber).ToListAsync();

/// <summary>One SaveChanges for the whole batch, so the caller's transaction covers it.</summary>
public async Task AddRangeAsync(IEnumerable<Member> members)
{
    _db.Members.AddRange(members);
    await _db.SaveChangesAsync();
}
```

In `ServicePackageRepository`, the same shape with `GetAllNamesAsync()` returning `Name` and `AddRangeAsync(IEnumerable<ServicePackage>)`. Declare both on the interfaces.

- [ ] **Step 2: Write the result type**

Create `Services/Import/ImportResult.cs`:

```csharp
using gym_management_server.Infrastructure.Excel;

namespace gym_management_server.Services.Import
{
    /// <summary>
    /// <paramref name="Committed"/> is false for a dry run and for any run that found errors.
    /// <paramref name="ImportedRows"/> is 0 unless <paramref name="Committed"/> is true —
    /// import is all-or-nothing.
    /// </summary>
    public record ImportResult(
        int TotalRows,
        int ImportedRows,
        bool Committed,
        IReadOnlyList<RowError> Errors)
    {
        public bool IsClean => Errors.Count == 0;
    }
}
```

- [ ] **Step 3: Write the member import service**

Create `Services/Import/MemberImportService.cs`:

```csharp
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
```

**Note the row-number arithmetic.** `FindDuplicatesAsync` indexes `read.Rows`, which excludes rows the parser rejected — so `i + 2` is only the true Excel row when nothing was rejected. That is fine: when the parser rejected anything, `errors.Count > 0` already, and the duplicate errors are reported alongside errors the user must fix first. Do not "improve" this into a wrong number.

- [ ] **Step 4: Write the service-package import service**

Create `Services/Import/ServicePackageImportService.cs` with the identical structure: read with `ServicePackageSheet.Import`, duplicate-check on `Name` against `GetAllNamesAsync()` and within the file, map `ServicePackageRow` to `ServicePackage` (`Id = Guid.NewGuid()`, `CreatedAt = DateTime.UtcNow`). The duplicate messages name the column `Tên gói` and read `Tên gói "{name}" đã tồn tại trong hệ thống.`

- [ ] **Step 5: Register both services**

In `Program.cs`, next to the other `AddScoped` registrations:

```csharp
builder.Services.AddScoped<MemberImportService>();
builder.Services.AddScoped<ServicePackageImportService>();
```

Add `using gym_management_server.Services.Import;`.

- [ ] **Step 6: Build**

```bash
dotnet build gym-management-server/gym-management-server.sln
```

Expected: succeeds. Tests for this service arrive in Task 4, which needs a database that honours transactions.

- [ ] **Step 7: Commit**

```bash
git add gym-management-server/
git commit -m "Add import services with database and in-file duplicate checks"
```

---

### Task 4: Import endpoints, with a database that tells the truth

**Files:**
- Modify: `Controllers/MemberController.cs`, `Controllers/ServicePackageController.cs`
- Create: `gym-management-server.Tests/Integration/SqliteWebApplicationFactory.cs`
- Test: `gym-management-server.Tests/Integration/ImportApiTests.cs`

**Interfaces:**
- Consumes: `MemberImportService`, `ServicePackageImportService`, `ExcelTemplateWriter`.
- Produces:
  - `GET /api/Member/import/template` → the `.xlsx` template, filename `mau-nhap-thanh-vien.xlsx`
  - `POST /api/Member/import?dryRun=true|false` → multipart with a `file` part, returns `ImportResult` as JSON
  - the same two on `ServicePackageController` (`mau-nhap-goi-dich-vu.xlsx`)
  - `SqliteWebApplicationFactory : WebApplicationFactory<Program>` exposing `CreateAuthenticatedClient(byte role)` and `Services`

Both endpoints carry `[Authorize]`. Reject a file over 5 MB with `400` before reading it.

- [ ] **Step 1: Add the SQLite test package**

```bash
dotnet add gym-management-server/gym-management-server.Tests/gym-management-server.Tests.csproj package Microsoft.EntityFrameworkCore.Sqlite --version 9.0.8
```

- [ ] **Step 2: Write the SQLite fixture**

Create `gym-management-server.Tests/Integration/SqliteWebApplicationFactory.cs`:

```csharp
using System.Data.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using gym_management_server.Data.EntityFramework;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace gym_management_server.Tests.Integration
{
    /// <summary>
    /// Boots the app against SQLite held open in memory.
    ///
    /// The EF Core InMemory provider used by CustomWebApplicationFactory is wrong for import
    /// tests: it ignores transactions (so a rollback test would pass without rolling anything
    /// back) and it does not enforce unique indexes (so a duplicate-phone test would pass with
    /// the duplicate check deleted). SQLite honours both. Do not swap this back.
    /// </summary>
    public class SqliteWebApplicationFactory : WebApplicationFactory<Program>
    {
        private DbConnection? _connection;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Fingerprint:EncryptionKey"] = Convert.ToBase64String(new byte[32]),
                }));

            builder.ConfigureServices(services =>
            {
                var toRemove = services.Where(d =>
                        d.ServiceType == typeof(DbContextOptions<GymManagementContext>) ||
                        d.ServiceType == typeof(DbContextOptions) ||
                        d.ServiceType == typeof(GymManagementContext) ||
                        (d.ServiceType.FullName?.StartsWith("Microsoft.EntityFrameworkCore") ?? false) ||
                        (d.ImplementationType?.FullName?.Contains("SqlServer") ?? false))
                    .ToList();
                foreach (var d in toRemove) services.Remove(d);

                // Kept open for the lifetime of the factory; closing it drops the database.
                _connection = new SqliteConnection("DataSource=:memory:");
                _connection.Open();

                services.AddDbContext<GymManagementContext>(options => options.UseSqlite(_connection));
            });

            builder.ConfigureServices(services =>
            {
                using var scope = services.BuildServiceProvider().CreateScope();
                scope.ServiceProvider.GetRequiredService<GymManagementContext>()
                     .Database.EnsureCreated();
            });
        }

        public HttpClient CreateAuthenticatedClient(byte role)
        {
            var client = CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", IssueToken(role));
            return client;
        }

        private string IssueToken(byte role)
        {
            var config = Services.GetRequiredService<IConfiguration>().GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Key"]!));

            var token = new JwtSecurityToken(
                issuer: config["Issuer"],
                audience: config["Audience"],
                claims: new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.Name, role == 1 ? "admin" : "staff"),
                    new Claim(ClaimTypes.Role, role.ToString()),
                },
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing) _connection?.Dispose();
        }
    }
}
```

If `EnsureCreated()` fails on the `HasFilter("[IsDeleted] = 0")` index (SQL Server bracket syntax SQLite rejects), catch that specific failure and adjust the two filtered indexes in `OnModelCreating` to unbracketed `"IsDeleted = 0"`, which both providers accept. Verify the backend still builds and the fingerprint tests still pass after such a change.

- [ ] **Step 3: Write the failing test**

Create `gym-management-server.Tests/Integration/ImportApiTests.cs`:

```csharp
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using ClosedXML.Excel;
using gym_management_server.Data.EntityFramework;
using gym_management_server.Entities.Members;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace gym_management_server.Tests.Integration
{
    public class ImportApiTests : IClassFixture<SqliteWebApplicationFactory>
    {
        private readonly SqliteWebApplicationFactory _factory;
        public ImportApiTests(SqliteWebApplicationFactory factory) => _factory = factory;

        private static MultipartFormDataContent FileWith(params string[][] dataRows)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Thành viên");
            var headers = new[] { "Họ và tên", "Số điện thoại", "Trạng thái" };
            for (var c = 0; c < headers.Length; c++) ws.Cell(1, c + 1).Value = headers[c];
            for (var r = 0; r < dataRows.Length; r++)
                for (var c = 0; c < dataRows[r].Length; c++)
                    ws.Cell(r + 2, c + 1).Value = dataRows[r][c];

            var stream = new MemoryStream();
            wb.SaveAs(stream);

            var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(stream.ToArray()), "file", "import.xlsx");
            return content;
        }

        private int MemberCount()
        {
            using var scope = _factory.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<GymManagementContext>().Members.Count();
        }

        [Fact]
        public async Task Import_requires_authentication()
        {
            var response = await _factory.CreateClient()
                .PostAsync("/api/Member/import?dryRun=true", FileWith(["A", "0900000001", "Hoạt động"]));
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Template_endpoint_returns_a_workbook_with_the_import_headers()
        {
            var response = await _factory.CreateAuthenticatedClient(0).GetAsync("/api/Member/import/template");
            response.EnsureSuccessStatusCode();

            using var wb = new XLWorkbook(new MemoryStream(await response.Content.ReadAsByteArrayAsync()));
            Assert.StartsWith("Họ và tên", wb.Worksheet("Thành viên").Cell(1, 1).GetString());
        }

        [Fact]
        public async Task A_dry_run_writes_nothing_even_when_the_file_is_perfect()
        {
            var before = MemberCount();

            var response = await _factory.CreateAuthenticatedClient(0)
                .PostAsync("/api/Member/import?dryRun=true", FileWith(["Nguyễn Văn A", "0900000011", "Hoạt động"]));
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.True(body.GetProperty("isClean").GetBoolean());
            Assert.False(body.GetProperty("committed").GetBoolean());
            Assert.Equal(0, body.GetProperty("importedRows").GetInt32());
            Assert.Equal(before, MemberCount());
        }

        [Fact]
        public async Task A_committed_run_writes_the_rows()
        {
            var before = MemberCount();

            var response = await _factory.CreateAuthenticatedClient(0)
                .PostAsync("/api/Member/import?dryRun=false",
                    FileWith(["Nguyễn Văn B", "0900000021", "Hoạt động"],
                             ["Trần Thị C", "0900000022", "Tạm ngưng"]));
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.True(body.GetProperty("committed").GetBoolean());
            Assert.Equal(2, body.GetProperty("importedRows").GetInt32());
            Assert.Equal(before + 2, MemberCount());
        }

        [Fact]
        public async Task One_bad_row_stops_the_whole_file()
        {
            var before = MemberCount();

            var response = await _factory.CreateAuthenticatedClient(0)
                .PostAsync("/api/Member/import?dryRun=false",
                    FileWith(["Hợp lệ", "0900000031", "Hoạt động"],
                             ["Sai trạng thái", "0900000032", "Đang nghỉ"]));
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.False(body.GetProperty("committed").GetBoolean());
            Assert.Equal(0, body.GetProperty("importedRows").GetInt32());
            Assert.Equal(before, MemberCount());   // the valid row must NOT have been written

            var error = body.GetProperty("errors")[0];
            Assert.Equal(3, error.GetProperty("rowNumber").GetInt32());
            Assert.Contains("Hoạt động", error.GetProperty("message").GetString());
        }

        [Fact]
        public async Task A_phone_number_already_in_the_database_is_rejected()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<GymManagementContext>();
                db.Members.Add(new Member { Id = Guid.NewGuid(), FullName = "Đã có", PhoneNumber = "0900000041" });
                db.SaveChanges();
            }

            var response = await _factory.CreateAuthenticatedClient(0)
                .PostAsync("/api/Member/import?dryRun=true", FileWith(["Trùng", "0900000041", "Hoạt động"]));
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.Contains("đã tồn tại", body.GetProperty("errors")[0].GetProperty("message").GetString());
        }

        [Fact]
        public async Task Two_rows_sharing_a_phone_number_are_rejected_before_the_database_sees_them()
        {
            var response = await _factory.CreateAuthenticatedClient(0)
                .PostAsync("/api/Member/import?dryRun=true",
                    FileWith(["Một", "0900000051", "Hoạt động"],
                             ["Hai", "0900000051", "Hoạt động"]));
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            var message = body.GetProperty("errors")[0].GetProperty("message").GetString();
            Assert.Contains("trùng với dòng 2", message);
        }
    }
}
```

- [ ] **Step 4: Run the test and confirm it fails**

```bash
dotnet test gym-management-server/gym-management-server.sln --filter FullyQualifiedName~ImportApiTests
```

Expected: 404s — the endpoints do not exist.

- [ ] **Step 5: Add the endpoints**

In `MemberController`, inject `MemberImportService` alongside the existing dependencies and add:

```csharp
private const long MaxUploadBytes = 5 * 1024 * 1024;

[HttpGet("import/template")]
[Authorize]
public IActionResult ImportTemplate() =>
    ExcelFileResult.File(ExcelTemplateWriter.Write(MemberSheet.Import), "mau-nhap-thanh-vien");

/// <summary>
/// dryRun=true validates and writes nothing. dryRun=false writes, but only if the file is
/// completely clean. There is no server-side state between the two calls: the client sends
/// the same file twice.
/// </summary>
[HttpPost("import")]
[Authorize]
public async Task<IActionResult> Import(IFormFile file, [FromQuery] bool dryRun = true)
{
    if (file is null || file.Length == 0)
        return BadRequest(new { message = "Chưa chọn file." });
    if (file.Length > MaxUploadBytes)
        return BadRequest(new { message = "File vượt quá 5 MB." });
    if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        return BadRequest(new { message = "Chỉ chấp nhận file .xlsx." });

    await using var stream = file.OpenReadStream();
    try
    {
        return Ok(await _importService.ImportAsync(stream, dryRun));
    }
    catch (Exception ex) when (ex is InvalidOperationException or FormatException)
    {
        return BadRequest(new { message = $"Không đọc được file: {ex.Message}" });
    }
}
```

Note the template's file name has no date suffix — `ExcelFileResult.File` appends one. That is acceptable for a template; do not add a second naming helper.

Mirror both endpoints in `ServicePackageController` using `ServicePackageImportService`, `ServicePackageSheet.Import` and `mau-nhap-goi-dich-vu`.

- [ ] **Step 6: Run the tests and confirm they pass**

```bash
dotnet test gym-management-server/gym-management-server.sln --filter FullyQualifiedName~ImportApiTests
```

Expected: 7 passing.

- [ ] **Step 7: Run the whole suite**

```bash
dotnet test gym-management-server/gym-management-server.sln
```

Expected: everything green, including the export and fingerprint tests.

- [ ] **Step 8: Commit**

```bash
git add gym-management-server/
git commit -m "Add import endpoints, tested against SQLite so transactions are real"
```

---

### Task 5: The import dialog

**Files:**
- Create: `gym-management-client/src/app/shared/services/import.service.ts`
- Create: `gym-management-client/src/app/shared/components/import-dialog/import-dialog.component.ts`, `.html`, `.scss`
- Create: `gym-management-client/src/app/shared/components/import-button/import-button.component.ts`, `.html`
- Modify: `gym-management-client/src/app/app.module.ts`
- Modify: `member-list.component.html`, `service-package-list.component.html` (and their `.ts` to refresh the grid after a successful import)
- Test: `gym-management-client/src/app/shared/services/import.service.spec.ts`

**Interfaces:**
- Consumes: the import endpoints from Task 4; `FileDownloadService` from the export plan (for the template button).
- Produces:
  - `interface RowError { rowNumber: number; columnHeader: string | null; message: string }`
  - `interface ImportResult { totalRows: number; importedRows: number; committed: boolean; isClean: boolean; errors: RowError[] }`
  - `ImportService.upload(baseUrl: string, file: File, dryRun: boolean): Observable<ImportResult>`
  - `<app-import-button [baseUrl]="'/api/Member'" [title]="'Nhập thành viên từ Excel'" (imported)="grid.instance.refresh()">`

- [ ] **Step 1: Write the failing test**

Create `gym-management-client/src/app/shared/services/import.service.spec.ts`:

```typescript
import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { ImportService } from './import.service';

describe('ImportService', () => {
    let service: ImportService;
    let http: HttpTestingController;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [ImportService, provideHttpClient(), provideHttpClientTesting()],
        });
        service = TestBed.inject(ImportService);
        http = TestBed.inject(HttpTestingController);
    });

    afterEach(() => http.verify());

    it('posts the file as multipart with the dryRun flag', () => {
        const file = new File(['x'], 'members.xlsx');

        service.upload('/api/Member', file, true).subscribe();

        const req = http.expectOne(r => r.url === '/api/Member/import');
        expect(req.request.params.get('dryRun')).toBe('true');
        expect(req.request.body instanceof FormData).toBeTrue();
        expect((req.request.body as FormData).get('file')).toBe(file);
        req.flush({ totalRows: 1, importedRows: 0, committed: false, isClean: true, errors: [] });
    });

    it('sends dryRun=false on commit', () => {
        service.upload('/api/Member', new File(['x'], 'm.xlsx'), false).subscribe();

        const req = http.expectOne(r => r.url === '/api/Member/import');
        expect(req.request.params.get('dryRun')).toBe('false');
        req.flush({ totalRows: 1, importedRows: 1, committed: true, isClean: true, errors: [] });
    });
});
```

- [ ] **Step 2: Run the test and confirm it fails**

```bash
cd gym-management-client && npm test -- --watch=false --browsers=ChromeHeadless
```

Expected: cannot resolve `./import.service`.

- [ ] **Step 3: Write the service**

Create `gym-management-client/src/app/shared/services/import.service.ts`:

```typescript
import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface RowError {
    rowNumber: number;
    columnHeader: string | null;
    message: string;
}

export interface ImportResult {
    totalRows: number;
    importedRows: number;
    committed: boolean;
    isClean: boolean;
    errors: RowError[];
}

@Injectable({ providedIn: 'root' })
export class ImportService {
    constructor(private http: HttpClient) { }

    upload(baseUrl: string, file: File, dryRun: boolean): Observable<ImportResult> {
        const form = new FormData();
        form.append('file', file);
        return this.http.post<ImportResult>(`${baseUrl}/import`, form, {
            params: new HttpParams().set('dryRun', String(dryRun)),
        });
    }
}
```

- [ ] **Step 4: Run the test and confirm it passes**

```bash
cd gym-management-client && npm test -- --watch=false --browsers=ChromeHeadless
```

Expected: 2 passing.

- [ ] **Step 5: Write the dialog**

Create `gym-management-client/src/app/shared/components/import-dialog/import-dialog.component.ts`:

```typescript
import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { ImportResult, ImportService } from '../../services/import.service';
import { FileDownloadService } from '../../services/file-download.service';

export interface ImportDialogData {
    baseUrl: string;
    title: string;
    templateName: string;
}

type Stage = 'pick' | 'checked' | 'done';

@Component({
    selector: 'app-import-dialog',
    standalone: false,
    templateUrl: './import-dialog.component.html',
    styleUrls: ['./import-dialog.component.scss'],
})
export class ImportDialogComponent {
    stage: Stage = 'pick';
    file: File | null = null;
    result: ImportResult | null = null;
    busy = false;
    failure: string | null = null;

    constructor(
        @Inject(MAT_DIALOG_DATA) public data: ImportDialogData,
        private dialogRef: MatDialogRef<ImportDialogComponent, number>,
        private imports: ImportService,
        private downloads: FileDownloadService
    ) { }

    /** Enabled only once a dry run came back with zero errors. */
    get canCommit(): boolean {
        return this.stage === 'checked' && !!this.result?.isClean && this.result.totalRows > 0;
    }

    downloadTemplate(): void {
        this.downloads
            .download(`${this.data.baseUrl}/import/template`, this.data.templateName)
            .subscribe();
    }

    pick(event: Event): void {
        this.file = (event.target as HTMLInputElement).files?.[0] ?? null;
        this.stage = 'pick';
        this.result = null;
        this.failure = null;
    }

    check(): void {
        this.run(true, () => { this.stage = 'checked'; });
    }

    commit(): void {
        // The same file is sent again: the server keeps no state between the two calls.
        this.run(false, () => {
            this.stage = 'done';
            this.dialogRef.close(this.result?.importedRows ?? 0);
        });
    }

    private run(dryRun: boolean, onSuccess: () => void): void {
        if (!this.file) return;
        this.busy = true;
        this.failure = null;

        this.imports.upload(this.data.baseUrl, this.file, dryRun).subscribe({
            next: (result) => {
                this.result = result;
                this.busy = false;
                onSuccess();
            },
            error: (err) => {
                this.busy = false;
                this.failure = err.error?.message ?? 'Nhập dữ liệu thất bại.';
            },
        });
    }
}
```

`import-dialog.component.html`:

```html
<h2 mat-dialog-title>{{ data.title }}</h2>

<mat-dialog-content>
  <button mat-stroked-button (click)="downloadTemplate()">
    <mat-icon>download</mat-icon> Tải file mẫu
  </button>

  <input type="file" accept=".xlsx" (change)="pick($event)" [disabled]="busy" />

  <p *ngIf="failure" class="failure">{{ failure }}</p>

  <ng-container *ngIf="result">
    <p *ngIf="result.isClean" class="ok">
      Đã kiểm tra {{ result.totalRows }} dòng, không có lỗi. Bấm "Nhập dữ liệu" để ghi vào hệ thống.
    </p>

    <ng-container *ngIf="!result.isClean">
      <p class="failure">
        Tìm thấy {{ result.errors.length }} lỗi. Không dòng nào được nhập cho tới khi sửa hết.
      </p>
      <table class="errors">
        <tr><th>Dòng</th><th>Cột</th><th>Lỗi</th></tr>
        <tr *ngFor="let e of result.errors">
          <td>{{ e.rowNumber }}</td>
          <td>{{ e.columnHeader }}</td>
          <td>{{ e.message }}</td>
        </tr>
      </table>
    </ng-container>
  </ng-container>
</mat-dialog-content>

<mat-dialog-actions align="end">
  <button mat-button mat-dialog-close [disabled]="busy">Đóng</button>
  <button mat-button (click)="check()" [disabled]="!file || busy">Kiểm tra</button>
  <button mat-raised-button color="primary" (click)="commit()" [disabled]="!canCommit || busy">
    Nhập dữ liệu
  </button>
</mat-dialog-actions>
```

`import-dialog.component.scss`:

```scss
input[type='file'] { display: block; margin: 16px 0; }
.failure { color: #b3261e; }
.ok { color: #1b5e20; }
.errors {
  width: 100%;
  border-collapse: collapse;
  th, td { border-bottom: 1px solid rgba(0, 0, 0, 0.12); padding: 6px 8px; text-align: left; }
  th { font-weight: 600; }
}
```

- [ ] **Step 6: Write the trigger button**

Create `gym-management-client/src/app/shared/components/import-button/import-button.component.ts`:

```typescript
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ImportDialogComponent, ImportDialogData } from '../import-dialog/import-dialog.component';

@Component({
    selector: 'app-import-button',
    standalone: false,
    templateUrl: './import-button.component.html',
})
export class ImportButtonComponent {
    @Input({ required: true }) baseUrl!: string;
    @Input({ required: true }) title!: string;
    @Input({ required: true }) templateName!: string;
    @Output() imported = new EventEmitter<number>();

    constructor(private dialog: MatDialog, private snackBar: MatSnackBar) { }

    open(): void {
        const data: ImportDialogData = {
            baseUrl: this.baseUrl,
            title: this.title,
            templateName: this.templateName,
        };

        this.dialog
            .open(ImportDialogComponent, { width: '760px', data })
            .afterClosed()
            .subscribe((importedRows?: number) => {
                if (!importedRows) return;
                this.snackBar.open(`Đã nhập ${importedRows} dòng.`, 'OK', { duration: 4000 });
                this.imported.emit(importedRows);
            });
    }
}
```

`import-button.component.html`:

```html
<button mat-stroked-button color="primary" (click)="open()">
  <mat-icon>upload_file</mat-icon> Nhập Excel
</button>
```

- [ ] **Step 7: Declare the three components**

In `app.module.ts`, add `ImportDialogComponent` and `ImportButtonComponent` to `declarations` and `exports`, mirroring how `ExportButtonComponent` was added in the export plan. Confirm `MatDialogModule`, `MatTableModule` are already imported — `member-list` already uses `MatDialog`, so the module is present.

- [ ] **Step 8: Wire the button into the two grids**

In `member-list.component.html`, next to the export button:

```html
<app-import-button
  [baseUrl]="'/api/Member'"
  [title]="'Nhập thành viên từ Excel'"
  [templateName]="'mau-nhap-thanh-vien.xlsx'"
  (imported)="reload()">
</app-import-button>
```

In `member-list.component.ts`, add a `@ViewChild(DxDataGridComponent) grid!: DxDataGridComponent;` and:

```typescript
reload(): void {
    this.grid?.instance?.refresh();
}
```

Repeat for `service-package-list` with `/api/ServicePackage`, `'Nhập gói dịch vụ từ Excel'`, `'mau-nhap-goi-dich-vu.xlsx'`.

- [ ] **Step 9: Build and verify by hand**

```bash
cd gym-management-client && npm run build
```

Run both halves, then walk the whole path: open Thành viên → **Nhập Excel** → **Tải file mẫu** → fill two rows, one of them with a deliberately wrong status → **Kiểm tra**. Confirm the error table names the right Excel row and that the "Nhập dữ liệu" button stays disabled. Fix the row in the file, re-pick it, check again, commit, and confirm the grid shows the new members.

- [ ] **Step 10: Commit**

```bash
git add gym-management-client/
git commit -m "Add the Excel import dialog for members and service packages"
```

---

## Self-Review

**Spec coverage**

| Spec section | Task |
|---|---|
| 6.1 template / dryRun / commit endpoints | 2 (template writer), 4 (endpoints) |
| 6.2 no server-side state between the two steps | 4 step 5 (endpoint comment), 5 step 5 (`commit()` re-sends) |
| 6.3 missing columns, bad formats, bad enum values | 1, 2 |
| 6.3 duplicates against the database | 3 |
| 6.3 duplicates within the file | 3 |
| 6.3 errors quote the Excel row number | 1, 3, verified in 4 |
| 6.4 all-or-nothing, `AddRangeAsync`, transaction | 3, proven in 4 |
| 6.5 shared dialog for both modules | 5 |
| 4.1 `ExcelReader`, `RowError`, `ReadResult` | 1 |
| 9 unit + integration tests | every task |

**Gaps found and closed:**
- The spec says the import is all-or-nothing and duplicate-checked, but the existing test factory uses the EF InMemory provider, which enforces neither. Tests written against it would have passed while proving nothing. Task 4 introduces a SQLite fixture and says explicitly why.
- The template writer marks required headers with ` *`, which would have broken the reader's by-name header match. Task 2 step 5 adds the matching trim to `ExcelReader`, and step 6 re-runs the reader tests to prove it.

**Type consistency:** `ImportResult`'s four properties are spelled the same in the C# record (Task 3), the JSON assertions (Task 4) and the TypeScript interface (Task 5) — `totalRows`, `importedRows`, `committed`, `errors`, plus the computed `isClean`. `RowError`'s `rowNumber` / `columnHeader` / `message` likewise. `ImportService.upload(baseUrl, file, dryRun)` matches its test and both call sites. `ExcelReader.Read<T>(Stream, SheetDefinition<T>)` is called identically in Tasks 1 and 3.
