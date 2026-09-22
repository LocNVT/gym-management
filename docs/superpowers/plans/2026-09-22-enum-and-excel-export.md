# Enum Normalisation + Excel Export Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give all nine data modules a working "export to Excel" button, built on a shared column-declaration layer, and fix the status enums the export (and the later dashboard) depend on.

**Architecture:** Status values move from bare `byte` to `: byte` enums carrying `[Description]` Vietnamese labels — one source of truth serving the API, the Excel files and the Angular dropdowns. A small `Infrastructure/Excel` layer turns a declarative `SheetDefinition<T>` into an `.xlsx` byte array. Each module declares its own columns next to its own service. Export reads the full filtered set, never a page.

**Tech Stack:** ASP.NET Core 8, EF Core 9, ClosedXML, xUnit, Angular 20, DevExtreme DataGrid.

**Spec:** `docs/superpowers/specs/2026-09-22-excel-dashboard-design.md`

## Global Constraints

- Target framework is `net8.0` for both projects. Do not retarget.
- Backend solution: `gym-management-server/gym-management-server.sln`. Run tests with `dotnet test` from `gym-management-server/`.
- Test framework is xUnit with `[Fact]` / `[Theory]`. Integration tests boot the app through `CustomWebApplicationFactory` (EF Core InMemory provider).
- All new endpoints carry `[Authorize]`. **An `/export` endpoint's authorisation must exactly match the `GET` on the same controller** — never looser.
- Excel export is capped at **50,000 rows**; over that, return `400`.
- Vietnamese labels are the UI language. Code identifiers stay English.
- `FingerprintTemplate` must never get an export endpoint. This is deliberate, not an oversight.
- Repositories return materialised `List<T>`; do not leak `IQueryable` out of the repository layer.
- Do not reformat or restructure files beyond what a task calls for.

## Divergence From The Spec (read this first)

The spec's section 4.2 example declared columns over `MemberOutput`. While planning, three facts in the codebase made that unworkable:

1. `MemberDataServiceOutput.Member` is typed `MemberOutput` but the entity's property is `Member`. `GymManagementServiceMapObjects` only copies properties whose types match **exactly**, so that field is always `null`. Export needs the member's name.
2. `CheckInOutput` exposes the **entity** `Member`, not a DTO.
3. `MemberDataServiceOutput` and `CheckInOutput` live in the `Entities.*` namespace, not `DTOs.*`.

So export projects into a **dedicated row type per module** (`MemberRow`, `InvoiceRow`, …) built by the repository with explicit joins, rather than reusing the CRUD output DTOs. This also satisfies the spec's "import columns are a subset of export columns" rule cleanly, because import (plan 2) parses into the same row type.

The pre-existing DTO namespace and null-`Member` problems are **left alone** — they are not this plan's job.

---

### Task 1: Status enums and the label helper

**Files:**
- Create: `gym-management-server/gym-management-server/Entities/Enums/StatusEnums.cs`
- Create: `gym-management-server/gym-management-server/Infrastructure/Enums/EnumLabel.cs`
- Test: `gym-management-server/gym-management-server.Tests/Unit/EnumLabelTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: enums `MemberStatus`, `InvoiceStatus`, `SubscriptionStatus`, `PaymentMethod`, `TrainerStatus`, `Gender`, `CheckInMethod` (all `: byte`, namespace `gym_management_server.Entities.Enums`). Helper `gym_management_server.Infrastructure.Enums.EnumLabel` with:
  - `static string ToLabel<TEnum>(this TEnum value) where TEnum : struct, Enum`
  - `static string ToLabel<TEnum>(this TEnum? value) where TEnum : struct, Enum` — returns `""` for null
  - `static IReadOnlyList<EnumValueInfo> Describe<TEnum>() where TEnum : struct, Enum`
  - `static bool TryParseLabel<TEnum>(string label, out TEnum result) where TEnum : struct, Enum` — case- and whitespace-insensitive
  - `record EnumValueInfo(byte Value, string Name, string Label)`

- [ ] **Step 1: Write the failing test**

Create `gym-management-server.Tests/Unit/EnumLabelTests.cs`:

```csharp
using gym_management_server.Entities.Enums;
using gym_management_server.Infrastructure.Enums;
using Xunit;

namespace gym_management_server.Tests.Unit
{
    public class EnumLabelTests
    {
        [Fact]
        public void ToLabel_returns_the_Description_attribute()
        {
            Assert.Equal("Đã thanh toán", InvoiceStatus.Paid.ToLabel());
            Assert.Equal("Chờ thanh toán", InvoiceStatus.Pending.ToLabel());
            Assert.Equal("Hoạt động", MemberStatus.Active.ToLabel());
        }

        [Fact]
        public void ToLabel_on_null_returns_empty_string()
        {
            Gender? missing = null;
            Assert.Equal("", missing.ToLabel());
        }

        [Fact]
        public void Describe_lists_every_value_with_its_numeric_value()
        {
            var values = EnumLabel.Describe<InvoiceStatus>();

            Assert.Equal(3, values.Count);
            Assert.Equal((byte)0, values[0].Value);
            Assert.Equal("Pending", values[0].Name);
            Assert.Equal("Chờ thanh toán", values[0].Label);
        }

        [Theory]
        [InlineData("Đã thanh toán", InvoiceStatus.Paid)]
        [InlineData("  đã thanh toán  ", InvoiceStatus.Paid)]
        [InlineData("ĐÃ HỦY", InvoiceStatus.Cancelled)]
        public void TryParseLabel_accepts_any_casing_and_surrounding_space(string input, InvoiceStatus expected)
        {
            Assert.True(EnumLabel.TryParseLabel<InvoiceStatus>(input, out var result));
            Assert.Equal(expected, result);
        }

        [Fact]
        public void TryParseLabel_rejects_an_unknown_label()
        {
            Assert.False(EnumLabel.TryParseLabel<InvoiceStatus>("Chưa rõ", out _));
        }

        [Fact]
        public void Enum_numeric_values_match_the_labels_the_Angular_grids_already_use()
        {
            // These numbers are already in the database and in the Angular components.
            // Changing them would silently reinterpret existing rows.
            Assert.Equal(0, (byte)MemberStatus.Active);
            Assert.Equal(1, (byte)MemberStatus.Suspended);
            Assert.Equal(0, (byte)InvoiceStatus.Pending);
            Assert.Equal(1, (byte)InvoiceStatus.Paid);
            Assert.Equal(0, (byte)SubscriptionStatus.Active);
            Assert.Equal(1, (byte)SubscriptionStatus.Expired);
            Assert.Equal(2, (byte)CheckInMethod.Fingerprint);
        }
    }
}
```

- [ ] **Step 2: Run the test and confirm it fails**

```bash
dotnet test gym-management-server/gym-management-server.sln --filter FullyQualifiedName~EnumLabelTests
```

Expected: compile error — `MemberStatus` / `EnumLabel` do not exist.

- [ ] **Step 3: Write the enums**

Create `Entities/Enums/StatusEnums.cs`:

```csharp
using System.ComponentModel;

namespace gym_management_server.Entities.Enums
{
    // Numeric values are load-bearing: they are already stored in the database
    // and hard-coded in the Angular grid components. Never renumber them.

    public enum MemberStatus : byte
    {
        [Description("Hoạt động")] Active = 0,
        [Description("Tạm ngưng")] Suspended = 1,
        [Description("Hết hạn")] Expired = 2,
    }

    public enum InvoiceStatus : byte
    {
        [Description("Chờ thanh toán")] Pending = 0,
        [Description("Đã thanh toán")] Paid = 1,
        [Description("Đã hủy")] Cancelled = 2,
    }

    public enum SubscriptionStatus : byte
    {
        [Description("Hoạt động")] Active = 0,
        [Description("Hết hạn")] Expired = 1,
        [Description("Đã hủy")] Cancelled = 2,
    }

    public enum PaymentMethod : byte
    {
        [Description("Tiền mặt")] Cash = 0,
        [Description("Chuyển khoản")] BankTransfer = 1,
        [Description("Thẻ")] Card = 2,
    }

    public enum TrainerStatus : byte
    {
        [Description("Đang làm việc")] Working = 0,
        [Description("Ngừng làm việc")] Inactive = 1,
    }

    public enum Gender : byte
    {
        [Description("Nam")] Male = 0,
        [Description("Nữ")] Female = 1,
        [Description("Khác")] Other = 2,
    }

    public enum CheckInMethod : byte
    {
        [Description("Không rõ")] Unknown = 0,
        [Description("Thẻ")] Card = 1,
        [Description("Vân tay")] Fingerprint = 2,
    }
}
```

- [ ] **Step 4: Write the label helper**

Create `Infrastructure/Enums/EnumLabel.cs`:

```csharp
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;

namespace gym_management_server.Infrastructure.Enums
{
    public record EnumValueInfo(byte Value, string Name, string Label);

    /// <summary>
    /// Reads the [Description] label off enum members. Results are cached per enum type —
    /// this runs once per cell of an exported spreadsheet, so reflecting every time would show.
    /// </summary>
    public static class EnumLabel
    {
        private static readonly ConcurrentDictionary<Type, IReadOnlyList<EnumValueInfo>> Cache = new();

        public static IReadOnlyList<EnumValueInfo> Describe<TEnum>() where TEnum : struct, Enum =>
            Cache.GetOrAdd(typeof(TEnum), static type =>
                type.GetFields(BindingFlags.Public | BindingFlags.Static)
                    .Select(f => new EnumValueInfo(
                        Value: Convert.ToByte(f.GetRawConstantValue()),
                        Name: f.Name,
                        Label: f.GetCustomAttribute<DescriptionAttribute>()?.Description ?? f.Name))
                    .OrderBy(v => v.Value)
                    .ToList());

        public static string ToLabel<TEnum>(this TEnum value) where TEnum : struct, Enum
        {
            var numeric = Convert.ToByte(value);
            return Describe<TEnum>().FirstOrDefault(v => v.Value == numeric)?.Label ?? numeric.ToString();
        }

        public static string ToLabel<TEnum>(this TEnum? value) where TEnum : struct, Enum =>
            value.HasValue ? value.Value.ToLabel() : string.Empty;

        public static bool TryParseLabel<TEnum>(string label, out TEnum result) where TEnum : struct, Enum
        {
            result = default;
            if (string.IsNullOrWhiteSpace(label)) return false;

            var needle = label.Trim();
            var match = Describe<TEnum>().FirstOrDefault(v =>
                string.Equals(v.Label, needle, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(v.Name, needle, StringComparison.OrdinalIgnoreCase));

            if (match is null) return false;
            result = (TEnum)Enum.ToObject(typeof(TEnum), match.Value);
            return true;
        }
    }
}
```

- [ ] **Step 5: Run the tests and confirm they pass**

```bash
dotnet test gym-management-server/gym-management-server.sln --filter FullyQualifiedName~EnumLabelTests
```

Expected: 6 passing.

- [ ] **Step 6: Commit**

```bash
git add gym-management-server/gym-management-server/Entities/Enums/ gym-management-server/gym-management-server/Infrastructure/Enums/ gym-management-server/gym-management-server.Tests/Unit/EnumLabelTests.cs
git commit -m "Add status enums with Vietnamese labels and a cached label helper"
```

---

### Task 2: Move entities and DTOs onto the enums, fix the wrong defaults

**Files:**
- Modify: `Entities/Members/Member.cs`, `Entities/Invoices/Invoice.cs`, `Entities/MemberDataServices/MemberDataService.cs`, `Entities/Trainers/Trainer.cs`, `Entities/Expenses/Expense.cs`, `Entities/CheckIns/CheckIn.cs`
- Modify: `DTOs/Members/MemberOutput.cs`, `DTOs/Members/MemberInput.cs`, `DTOs/Invoices/InvoiceOutput.cs`, `DTOs/Invoices/InvoiceInput.cs`, `DTOs/MemberDataServices/MemberDataServiceOutput.cs`, `DTOs/MemberDataServices/MemberDataServiceInput.cs`, `DTOs/Trainers/TrainerOutput.cs`, `DTOs/Trainers/TrainerInput.cs`, `DTOs/Expenses/ExpenseOutput.cs`, `DTOs/Expenses/ExpenseInput.cs`, `DTOs/CheckIns/CheckInOutput.cs`, `DTOs/CheckIns/CheckInCreateInput.cs`, `DTOs/CheckIns/CheckInUpdateInput.cs`
- Modify: `Services/Fingerprints/FingerprintService.cs` (uses `CheckInMethod.Fingerprint`)
- Create: `docs/sql/2026-09-22-fix-status-defaults.sql`
- Test: `gym-management-server.Tests/Unit/StatusMappingTests.cs`

**Interfaces:**
- Consumes: the enums from Task 1.
- Produces: `Member.Status` is `MemberStatus`, `Member.Gender` is `Gender?`, `Invoice.Status` is `InvoiceStatus`, `Invoice.PaymentMethod` / `Expense.PaymentMethod` are `PaymentMethod`, `MemberDataService.Status` is `SubscriptionStatus`, `Trainer.Status` is `TrainerStatus`, `CheckIn.Method` is `CheckInMethod`. The matching Input/Output DTO properties change to the same types.

**Why the DTOs must change in the same commit:** `GymManagementServiceMapObjects.MapObjects` copies a property only when `targetProp.PropertyType == sourceProp.PropertyType`. Change the entity to `MemberStatus` while the DTO stays `byte` and the status is **silently dropped** on every read and write — no exception, no warning, just a zero. Step 1's test exists to catch exactly that.

**No migration is needed.** A `: byte` enum maps to the same `tinyint` column, and the old defaults were CLR field initialisers, not `HasDefaultValue` in `OnModelCreating` (verified). Nothing about the schema changes.

- [ ] **Step 1: Write the failing test**

Create `gym-management-server.Tests/Unit/StatusMappingTests.cs`:

```csharp
using gym_management_server.DTOs.Members;
using gym_management_server.Entities.Enums;
using gym_management_server.Entities.Invoices;
using gym_management_server.Entities.MemberDataServices;
using gym_management_server.Entities.Members;
using gym_management_server.Services;
using Xunit;

namespace gym_management_server.Tests.Unit
{
    public class StatusMappingTests
    {
        [Fact]
        public void A_new_member_defaults_to_Active_not_Suspended()
        {
            Assert.Equal(MemberStatus.Active, new Member().Status);
        }

        [Fact]
        public void A_new_invoice_defaults_to_Pending_not_Paid()
        {
            Assert.Equal(InvoiceStatus.Pending, new Invoice().Status);
        }

        [Fact]
        public void A_new_subscription_defaults_to_Active_not_Expired()
        {
            Assert.Equal(SubscriptionStatus.Active, new MemberDataService().Status);
        }

        [Fact]
        public void Status_survives_the_reflection_mapper()
        {
            // The mapper copies a property only when the source and destination types match
            // exactly. If the entity and the DTO disagree, status is dropped silently.
            var mapper = new GymManagementServiceMapObjects();
            var member = new Member
            {
                FullName = "Nguyễn Văn A",
                PhoneNumber = "0900000001",
                Status = MemberStatus.Suspended,
                Gender = Gender.Female,
            };

            var output = mapper.MapObjects<Member, MemberOutput>(member);

            Assert.Equal(MemberStatus.Suspended, output.Status);
            Assert.Equal(Gender.Female, output.Gender);
        }
    }
}
```

- [ ] **Step 2: Run the test and confirm it fails**

```bash
dotnet test gym-management-server/gym-management-server.sln --filter FullyQualifiedName~StatusMappingTests
```

Expected: compile error — `Member.Status` is still `byte`, so it cannot be compared to `MemberStatus`.

- [ ] **Step 3: Change the entity properties**

In each entity, swap the type and drop the wrong default. For example, in `Entities/Members/Member.cs`:

```csharp
using gym_management_server.Entities.Enums;
// ...
public Gender? Gender { get; set; }
public MemberStatus Status { get; set; } = MemberStatus.Active;   // was: byte Status = 1
```

`Entities/Invoices/Invoice.cs`:

```csharp
public InvoiceStatus Status { get; set; } = InvoiceStatus.Pending;   // was: byte Status = 1
public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
```

`Entities/MemberDataServices/MemberDataService.cs`:

```csharp
public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;   // was: byte Status = 1
```

`Entities/Trainers/Trainer.cs`:

```csharp
public TrainerStatus Status { get; set; } = TrainerStatus.Working;
```

`Entities/Expenses/Expense.cs`:

```csharp
public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
```

`Entities/CheckIns/CheckIn.cs` — replace the `Method` property **and delete the `CheckInMethod` static class at the bottom of the file** (it is superseded by the enum):

```csharp
public CheckInMethod Method { get; set; } = CheckInMethod.Unknown;
```

Also update the constructor parameter `byte method` to `CheckInMethod method`.

- [ ] **Step 4: Change the matching DTO properties**

Apply the same type to every Input and Output DTO listed in **Files** above. `MemberOutput` / `MemberInput` also drop their `= 1` default on `Status`. Example, `DTOs/Invoices/InvoiceOutput.cs`:

```csharp
using gym_management_server.Entities.Enums;
// ...
public InvoiceStatus Status { get; set; }
public PaymentMethod PaymentMethod { get; set; }
```

The JSON wire format is unchanged — `System.Text.Json` serialises enums as their numeric value by default, so Angular still sends and receives `0` / `1` / `2`.

- [ ] **Step 5: Fix the one call site**

In `Services/Fingerprints/FingerprintService.cs`, `VerifyAsync` sets `Method = CheckInMethod.Fingerprint`. That line now refers to the enum instead of the deleted `const byte` — it compiles unchanged. Build to confirm nothing else referenced the old static class:

```bash
dotnet build gym-management-server/gym-management-server.sln
```

Expected: build succeeds. If any other file fails, it referenced `CheckInMethod.X` as a `byte`; change the local variable's type to `CheckInMethod`.

- [ ] **Step 6: Run the whole suite**

```bash
dotnet test gym-management-server/gym-management-server.sln
```

Expected: all pass, including the 28 pre-existing fingerprint tests. If a fingerprint test compares `Method` to a `byte` literal, update it to the enum member — do not change the assertion's meaning.

- [ ] **Step 7: Write the data-fix script (do not run it)**

Create `docs/sql/2026-09-22-fix-status-defaults.sql`:

```sql
-- Corrects rows written before 2026-09-22, when the C# defaults disagreed with the
-- labels shown in the UI: a new member was born "Suspended", a new invoice "Paid",
-- a new subscription "Expired".
--
-- DO NOT RUN THIS BLIND. There is no way to tell from the data whether an invoice with
-- Status = 1 was genuinely paid or merely defaulted. Review the rows first, and only run
-- the statements that match what actually happened in your gym.
--
-- Always take a backup first.

-- 1. Review before changing anything.
SELECT Status, COUNT(*) AS Rows, MIN(CreatedAt) AS Earliest, MAX(CreatedAt) AS Latest
FROM Members GROUP BY Status;

SELECT Status, COUNT(*) AS Rows, SUM(TotalAmount) AS Total, MIN(InvoiceDate) AS Earliest
FROM Invoices GROUP BY Status;

SELECT Status, COUNT(*) AS Rows, MIN(CreatedAt) AS Earliest FROM MemberDataServices GROUP BY Status;

-- 2. Members: rows never deliberately suspended should be Active (0).
-- UPDATE Members SET Status = 0 WHERE Status = 1 AND IsDeleted = 0;

-- 3. Subscriptions: a subscription whose EndDate is still in the future was never
-- genuinely "Expired" — it only looked that way because of the bad default.
-- UPDATE MemberDataServices SET Status = 0 WHERE Status = 1 AND EndDate > GETUTCDATE();

-- 4. Invoices: deliberately left commented out. Rewriting payment status changes the
-- books. Decide invoice by invoice.
-- UPDATE Invoices SET Status = 0 WHERE Status = 1 AND Id IN (...);
```

- [ ] **Step 8: Commit**

```bash
git add gym-management-server/ docs/sql/
git commit -m "Move status fields onto enums and correct the wrong defaults

New members were created Suspended, new invoices Paid and new subscriptions
Expired, because the C# defaults disagreed with the labels the Angular grids
show. The DTOs move in the same commit: the reflection mapper only copies
properties whose types match exactly, so a half-migration would silently drop
every status value."
```

---

### Task 3: Serve the enums to Angular and delete the duplicated option arrays

**Files:**
- Create: `gym-management-server/gym-management-server/Controllers/EnumsController.cs`
- Create: `gym-management-client/src/app/shared/services/enum.service.ts`
- Modify: `gym-management-client/src/app/modules/members/components/member-list/member-list.component.ts`, `.../invoices/components/invoice-list/invoice-list.component.ts`, `.../expenses/components/expense-list/expense-list.component.ts`, `.../member-data-service/components/member-data-service-list/member-data-service-list.component.ts`, `.../trainers/components/trainer-list/trainer-list.component.ts`, `.../check-in/components/check-in-list/check-in-list.component.ts`
- Test: `gym-management-server.Tests/Integration/EnumsApiTests.cs`

**Interfaces:**
- Consumes: `EnumLabel.Describe<TEnum>()` from Task 1.
- Produces: `GET /api/Enums` returning `Dictionary<string, IReadOnlyList<EnumValueInfo>>` keyed by enum name (`"memberStatus"`, `"invoiceStatus"`, `"subscriptionStatus"`, `"paymentMethod"`, `"trainerStatus"`, `"gender"`, `"checkInMethod"` — camelCase via the default JSON policy). Angular `EnumService.options(name): Observable<{value:number,text:string}[]>` shaped for DevExtreme `lookup`.

- [ ] **Step 1: Write the failing test**

Create `gym-management-server.Tests/Integration/EnumsApiTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace gym_management_server.Tests.Integration
{
    public class EnumsApiTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        public EnumsApiTests(CustomWebApplicationFactory factory) => _factory = factory;

        [Fact]
        public async Task Enums_endpoint_requires_authentication()
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/api/Enums");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Enums_endpoint_returns_every_enum_with_Vietnamese_labels()
        {
            var client = _factory.CreateAuthenticatedClient(role: 0);

            var payload = await client.GetFromJsonAsync<JsonElement>("/api/Enums");

            var invoiceStatus = payload.GetProperty("invoiceStatus");
            Assert.Equal(3, invoiceStatus.GetArrayLength());
            Assert.Equal(1, invoiceStatus[1].GetProperty("value").GetInt32());
            Assert.Equal("Đã thanh toán", invoiceStatus[1].GetProperty("label").GetString());

            foreach (var name in new[] { "memberStatus", "subscriptionStatus", "paymentMethod",
                                         "trainerStatus", "gender", "checkInMethod" })
                Assert.True(payload.TryGetProperty(name, out _), $"missing enum: {name}");
        }
    }
}
```

- [ ] **Step 2: Add the authenticated-client test helper**

`CustomWebApplicationFactory` has no way to make an authenticated request yet, and every later task needs one. Add to `gym-management-server.Tests/Integration/CustomWebApplicationFactory.cs`:

```csharp
// add these usings at the top of the file
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

// add these members inside the class
/// <summary>An HttpClient carrying a valid JWT. role: 0 = Staff, 1 = Admin.</summary>
public HttpClient CreateAuthenticatedClient(byte role)
{
    var client = CreateClient();
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", IssueToken(role));
    return client;
}

private string IssueToken(byte role)
{
    // Must match appsettings.json's Jwt section, which the test host loads as-is.
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
```

- [ ] **Step 3: Run the test and confirm it fails**

```bash
dotnet test gym-management-server/gym-management-server.sln --filter FullyQualifiedName~EnumsApiTests
```

Expected: the auth test passes (404 is not 401 — it will actually **fail** here), the payload test fails with 404. Both failures are expected; the endpoint does not exist.

- [ ] **Step 4: Write the controller**

Create `Controllers/EnumsController.cs`:

```csharp
using gym_management_server.Entities.Enums;
using gym_management_server.Infrastructure.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace gym_management_server.Controllers
{
    /// <summary>
    /// Single source of truth for status labels. The Angular grids read their dropdown
    /// options from here instead of each component hard-coding its own copy.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EnumsController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetAll() => Ok(new Dictionary<string, IReadOnlyList<EnumValueInfo>>
        {
            ["memberStatus"] = EnumLabel.Describe<MemberStatus>(),
            ["invoiceStatus"] = EnumLabel.Describe<InvoiceStatus>(),
            ["subscriptionStatus"] = EnumLabel.Describe<SubscriptionStatus>(),
            ["paymentMethod"] = EnumLabel.Describe<PaymentMethod>(),
            ["trainerStatus"] = EnumLabel.Describe<TrainerStatus>(),
            ["gender"] = EnumLabel.Describe<Gender>(),
            ["checkInMethod"] = EnumLabel.Describe<CheckInMethod>(),
        });
    }
}
```

- [ ] **Step 5: Run the test and confirm it passes**

```bash
dotnet test gym-management-server/gym-management-server.sln --filter FullyQualifiedName~EnumsApiTests
```

Expected: 2 passing.

- [ ] **Step 6: Add the Angular service**

Create `gym-management-client/src/app/shared/services/enum.service.ts`:

```typescript
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map, shareReplay } from 'rxjs';

export interface EnumValue { value: number; name: string; label: string; }
export type EnumMap = Record<string, EnumValue[]>;

/** DevExtreme lookup shape. */
export interface LookupOption { value: number; text: string; }

@Injectable({ providedIn: 'root' })
export class EnumService {
    // One request per app load; every grid shares the same response.
    private readonly all$: Observable<EnumMap> = this.http
        .get<EnumMap>('/api/Enums')
        .pipe(shareReplay({ bufferSize: 1, refCount: false }));

    constructor(private http: HttpClient) { }

    options(name: string): Observable<LookupOption[]> {
        return this.all$.pipe(
            map(all => (all[name] ?? []).map(v => ({ value: v.value, text: v.label })))
        );
    }
}
```

- [ ] **Step 7: Replace the hard-coded arrays in the six grid components**

In each component, delete the literal option array and resolve it from the service instead. `member-list.component.ts` becomes:

```typescript
// remove: genderOptions = [...]; statusOptions = [...];
genderOptions: LookupOption[] = [];
statusOptions: LookupOption[] = [];

constructor(
    private memberService: MemberService,
    private enumService: EnumService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar
) { }

ngOnInit(): void {
    this.enumService.options('gender').subscribe(o => this.genderOptions = o);
    this.enumService.options('memberStatus').subscribe(o => this.statusOptions = o);
    // ...existing CustomStore setup stays as-is
}
```

Map each component to its enum names:

| Component | Field → enum name |
|---|---|
| `member-list` | `genderOptions` → `gender`, `statusOptions` → `memberStatus` |
| `invoice-list` | `statusOptions` → `invoiceStatus`, `paymentMethodOptions` → `paymentMethod` |
| `expense-list` | `paymentMethodOptions` → `paymentMethod` (leave `categoryOptions` alone — it is free text, not an enum) |
| `member-data-service-list` | `statusOptions` → `subscriptionStatus` |
| `trainer-list` | `statusOptions` → `trainerStatus` |
| `check-in-list` | method options → `checkInMethod` |

Add `import { EnumService, LookupOption } from '../../../../shared/services/enum.service';` to each.

- [ ] **Step 8: Build the frontend**

```bash
cd gym-management-client && npm run build
```

Expected: build succeeds with no TypeScript errors.

- [ ] **Step 9: Commit**

```bash
git add gym-management-server/ gym-management-client/src/app/shared/services/enum.service.ts gym-management-client/src/app/modules/
git commit -m "Serve status labels from /api/Enums and drop the per-component copies"
```

---

### Task 4: The Excel writing layer

**Files:**
- Modify: `gym-management-server/gym-management-server/gym-management-server.csproj`
- Create: `Infrastructure/Excel/ExcelColumn.cs`, `Infrastructure/Excel/SheetDefinition.cs`, `Infrastructure/Excel/SheetPayload.cs`, `Infrastructure/Excel/ExcelWriter.cs`
- Test: `gym-management-server.Tests/Unit/ExcelWriterTests.cs`

**Interfaces:**
- Consumes: nothing from earlier tasks.
- Produces, in namespace `gym_management_server.Infrastructure.Excel`:
  - `sealed class ExcelColumn<T>` with ctor `(string header, Func<T, object?> get, string? format = null, double width = 18, bool isRequired = false, Action<T, string>? parse = null, IReadOnlyList<string>? allowedValues = null)` and matching read-only properties `Header`, `Get`, `Format`, `Width`, `IsRequired`, `Parse`, `AllowedValues`. `Parse` is `null` for read-only columns.
  - `sealed class SheetDefinition<T>` with ctor `(string sheetName, IReadOnlyList<ExcelColumn<T>> columns)`, properties `SheetName`, `Columns`, and `SheetDefinition<T> Plus(params ExcelColumn<T>[] extra)`.
  - `interface ISheetPayload { void WriteTo(XLWorkbook workbook); int RowCount { get; } }`
  - `sealed class SheetPayload<T>(SheetDefinition<T> sheet, IReadOnlyCollection<T> rows) : ISheetPayload`
  - `static class ExcelWriter` with `const int MaxRows = 50_000`, `byte[] Write(params ISheetPayload[] payloads)`, `byte[] Write<T>(SheetDefinition<T> sheet, IReadOnlyCollection<T> rows)`, and `const string ContentType`.
  - `class ExcelRowLimitExceededException(int actual) : Exception`

- [ ] **Step 1: Add the ClosedXML package**

```bash
dotnet add gym-management-server/gym-management-server/gym-management-server.csproj package ClosedXML --version 0.104.2
```

- [ ] **Step 2: Write the failing test**

Create `gym-management-server.Tests/Unit/ExcelWriterTests.cs`:

```csharp
using System.IO;
using ClosedXML.Excel;
using gym_management_server.Infrastructure.Excel;
using Xunit;

namespace gym_management_server.Tests.Unit
{
    public class ExcelWriterTests
    {
        private sealed class Row
        {
            public string Name { get; set; } = "";
            public decimal Amount { get; set; }
            public DateTime? When { get; set; }
        }

        private static SheetDefinition<Row> Sheet => new("Thử nghiệm", new[]
        {
            new ExcelColumn<Row>("Tên", r => r.Name, isRequired: true),
            new ExcelColumn<Row>("Số tiền", r => r.Amount, format: "#,##0"),
            new ExcelColumn<Row>("Ngày", r => r.When, format: "dd/MM/yyyy"),
        });

        private static IXLWorksheet Open(byte[] bytes, string sheetName)
            => new XLWorkbook(new MemoryStream(bytes)).Worksheet(sheetName);

        [Fact]
        public void Writes_the_headers_in_declaration_order()
        {
            var bytes = ExcelWriter.Write(Sheet, new[] { new Row { Name = "A" } });
            var ws = Open(bytes, "Thử nghiệm");

            Assert.Equal("Tên", ws.Cell(1, 1).GetString());
            Assert.Equal("Số tiền", ws.Cell(1, 2).GetString());
            Assert.Equal("Ngày", ws.Cell(1, 3).GetString());
        }

        [Fact]
        public void Writes_one_row_per_item_starting_at_row_2()
        {
            var rows = new[]
            {
                new Row { Name = "Nguyễn Văn A", Amount = 1500000m, When = new DateTime(2026, 3, 9) },
                new Row { Name = "Trần Thị B",   Amount = 250000m,  When = null },
            };

            var ws = Open(ExcelWriter.Write(Sheet, rows), "Thử nghiệm");

            Assert.Equal("Nguyễn Văn A", ws.Cell(2, 1).GetString());
            Assert.Equal(1500000m, ws.Cell(2, 2).GetValue<decimal>());
            Assert.Equal(new DateTime(2026, 3, 9), ws.Cell(2, 3).GetDateTime());
            Assert.Equal("Trần Thị B", ws.Cell(3, 1).GetString());
            Assert.True(ws.Cell(3, 3).IsEmpty());   // null must be a blank cell, not the text "null"
            Assert.True(ws.Cell(4, 1).IsEmpty());
        }

        [Fact]
        public void Applies_the_declared_number_format()
        {
            var ws = Open(ExcelWriter.Write(Sheet, new[] { new Row { Amount = 1m } }), "Thử nghiệm");
            Assert.Equal("#,##0", ws.Cell(2, 2).Style.NumberFormat.Format);
        }

        [Fact]
        public void Write_refuses_more_than_the_row_limit()
        {
            var tooMany = Enumerable.Range(0, ExcelWriter.MaxRows + 1).Select(_ => new Row()).ToList();
            Assert.Throws<ExcelRowLimitExceededException>(() => ExcelWriter.Write(Sheet, tooMany));
        }

        [Fact]
        public void Write_puts_each_payload_on_its_own_sheet()
        {
            var second = new SheetDefinition<Row>("Chi tiết", new[] { new ExcelColumn<Row>("Tên", r => r.Name) });

            var bytes = ExcelWriter.Write(
                new SheetPayload<Row>(Sheet, new[] { new Row { Name = "A" } }),
                new SheetPayload<Row>(second, new[] { new Row { Name = "B" } }));

            using var wb = new XLWorkbook(new MemoryStream(bytes));
            Assert.Equal(2, wb.Worksheets.Count);
            Assert.Equal("B", wb.Worksheet("Chi tiết").Cell(2, 1).GetString());
        }

        [Fact]
        public void Plus_appends_columns_and_leaves_the_original_untouched()
        {
            var extended = Sheet.Plus(new ExcelColumn<Row>("Ghi chú", _ => "x"));

            Assert.Equal(3, Sheet.Columns.Count);
            Assert.Equal(4, extended.Columns.Count);
            Assert.Equal("Ghi chú", extended.Columns[3].Header);
        }
    }
}
```

- [ ] **Step 3: Run the test and confirm it fails**

```bash
dotnet test gym-management-server/gym-management-server.sln --filter FullyQualifiedName~ExcelWriterTests
```

Expected: compile error — the `Infrastructure.Excel` types do not exist.

- [ ] **Step 4: Write the column and sheet types**

Create `Infrastructure/Excel/ExcelColumn.cs`:

```csharp
namespace gym_management_server.Infrastructure.Excel
{
    /// <summary>
    /// One spreadsheet column. The same declaration drives the export file, the import
    /// template and the upload parser, so the three can never drift apart.
    /// A column with a null <see cref="Parse"/> is read-only: it is written on export and
    /// ignored on import, which is what lets a freshly exported file be edited and
    /// re-imported without deleting the ID and timestamp columns first.
    /// </summary>
    public sealed class ExcelColumn<T>
    {
        public ExcelColumn(
            string header,
            Func<T, object?> get,
            string? format = null,
            double width = 18,
            bool isRequired = false,
            Action<T, string>? parse = null,
            IReadOnlyList<string>? allowedValues = null)
        {
            Header = header;
            Get = get;
            Format = format;
            Width = width;
            IsRequired = isRequired;
            Parse = parse;
            AllowedValues = allowedValues;
        }

        public string Header { get; }
        public Func<T, object?> Get { get; }
        public string? Format { get; }
        public double Width { get; }
        public bool IsRequired { get; }
        public Action<T, string>? Parse { get; }
        public IReadOnlyList<string>? AllowedValues { get; }
    }
}
```

Create `Infrastructure/Excel/SheetDefinition.cs`:

```csharp
namespace gym_management_server.Infrastructure.Excel
{
    public sealed class SheetDefinition<T>
    {
        public SheetDefinition(string sheetName, IReadOnlyList<ExcelColumn<T>> columns)
        {
            SheetName = sheetName;
            Columns = columns;
        }

        public string SheetName { get; }
        public IReadOnlyList<ExcelColumn<T>> Columns { get; }

        /// <summary>Returns a new definition with extra columns appended. Does not mutate this one.</summary>
        public SheetDefinition<T> Plus(params ExcelColumn<T>[] extra) =>
            new(SheetName, Columns.Concat(extra).ToList());
    }
}
```

- [ ] **Step 5: Write the payload and writer**

Create `Infrastructure/Excel/SheetPayload.cs`:

```csharp
using ClosedXML.Excel;

namespace gym_management_server.Infrastructure.Excel
{
    public interface ISheetPayload
    {
        int RowCount { get; }
        void WriteTo(XLWorkbook workbook);
    }

    public sealed class SheetPayload<T> : ISheetPayload
    {
        private readonly SheetDefinition<T> _sheet;
        private readonly IReadOnlyCollection<T> _rows;

        public SheetPayload(SheetDefinition<T> sheet, IReadOnlyCollection<T> rows)
        {
            _sheet = sheet;
            _rows = rows;
        }

        public int RowCount => _rows.Count;

        public void WriteTo(XLWorkbook workbook)
        {
            var ws = workbook.Worksheets.Add(_sheet.SheetName);

            for (var c = 0; c < _sheet.Columns.Count; c++)
            {
                var column = _sheet.Columns[c];
                var header = ws.Cell(1, c + 1);
                header.Value = column.Header;
                header.Style.Font.Bold = true;
                header.Style.Fill.BackgroundColor = XLColor.LightGray;
                ws.Column(c + 1).Width = column.Width;
            }

            var r = 2;
            foreach (var row in _rows)
            {
                for (var c = 0; c < _sheet.Columns.Count; c++)
                {
                    var column = _sheet.Columns[c];
                    var cell = ws.Cell(r, c + 1);
                    var value = column.Get(row);

                    if (value is null) continue;   // leave the cell genuinely empty
                    cell.Value = XLCellValue.FromObject(value);
                    if (column.Format is not null) cell.Style.NumberFormat.Format = column.Format;
                }
                r++;
            }

            ws.SheetView.FreezeRows(1);
            ws.RangeUsed()?.SetAutoFilter();
        }
    }
}
```

Create `Infrastructure/Excel/ExcelWriter.cs`:

```csharp
using ClosedXML.Excel;

namespace gym_management_server.Infrastructure.Excel
{
    public class ExcelRowLimitExceededException : Exception
    {
        public ExcelRowLimitExceededException(int actual)
            : base($"Kết quả có {actual:N0} dòng, vượt giới hạn {ExcelWriter.MaxRows:N0} dòng mỗi lần xuất. Vui lòng thu hẹp bộ lọc.")
            => Actual = actual;

        public int Actual { get; }
    }

    public static class ExcelWriter
    {
        /// <summary>ClosedXML builds the whole workbook in memory, so an unbounded export
        /// is a way to exhaust the server's RAM with one request.</summary>
        public const int MaxRows = 50_000;

        public const string ContentType =
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        public static byte[] Write<T>(SheetDefinition<T> sheet, IReadOnlyCollection<T> rows) =>
            Write(new SheetPayload<T>(sheet, rows));

        public static byte[] Write(params ISheetPayload[] payloads)
        {
            var total = payloads.Sum(p => p.RowCount);
            if (total > MaxRows) throw new ExcelRowLimitExceededException(total);

            using var workbook = new XLWorkbook();
            foreach (var payload in payloads) payload.WriteTo(workbook);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
```

- [ ] **Step 6: Run the tests and confirm they pass**

```bash
dotnet test gym-management-server/gym-management-server.sln --filter FullyQualifiedName~ExcelWriterTests
```

Expected: 6 passing.

- [ ] **Step 7: Commit**

```bash
git add gym-management-server/
git commit -m "Add a declarative Excel writing layer with a 50k row cap"
```

---

### Task 5: Export row types and sheet definitions

**Files:**
- Create: `Services/Members/MemberSheet.cs`, `Services/ServicePackages/ServicePackageSheet.cs`, `Services/Trainers/TrainerSheet.cs`, `Services/Devices/AttendanceDeviceSheet.cs`, `Services/Expenses/ExpenseSheet.cs`, `Services/Invoices/InvoiceSheet.cs`, `Services/MemberDataServices/MemberDataServiceSheet.cs`, `Services/CheckIns/CheckInSheet.cs`
- Create: `DTOs/Export/ExportRows.cs`
- Test: `gym-management-server.Tests/Unit/SheetDefinitionTests.cs`

**Interfaces:**
- Consumes: `ExcelColumn<T>`, `SheetDefinition<T>` (Task 4); `EnumLabel.ToLabel` (Task 1); the enums (Task 1).
- Produces, in `gym_management_server.DTOs.Export`, mutable classes (settable properties, parameterless ctor — plan 2's reader needs to construct and fill them):
  - `MemberRow { string FullName; string PhoneNumber; string? Email; DateTime? DateOfBirth; Gender? Gender; string? Address; string? EmergencyName; string? EmergencyPhone; MemberStatus Status; string? Notes; Guid Id; DateTime RegistrationDate; DateTime CreatedAt; }`
  - `ServicePackageRow { string Name; string? Description; decimal Price; int DurationDays; int? MaxCheckins; bool IsActive; Guid Id; DateTime CreatedAt; }`
  - `TrainerRow { Guid Id; string FullName; string? PhoneNumber; string? Email; string? Specialty; decimal HourlyRate; TrainerStatus Status; string? Notes; }`
  - `AttendanceDeviceRow { Guid Id; string Name; string? Location; string Vendor; string? SerialNumber; bool IsActive; DateTime CreatedAt; }`
  - `ExpenseRow { Guid Id; DateTime ExpenseDate; string Category; string? Description; decimal Amount; PaymentMethod PaymentMethod; string? Notes; }`
  - `InvoiceRow { Guid Id; string InvoiceNumber; string? MemberName; string? MemberPhone; DateTime InvoiceDate; decimal TotalAmount; InvoiceStatus Status; PaymentMethod PaymentMethod; string? Notes; }`
  - `InvoiceItemRow { string InvoiceNumber; string Description; int Quantity; decimal UnitPrice; decimal LineTotal; }`
  - `SubscriptionRow { Guid Id; string MemberName; string MemberPhone; string PackageName; DateTime StartDate; DateTime EndDate; decimal PriceAtPurchase; int? RemainingCheckins; SubscriptionStatus Status; }`
  - `AttendanceRow { Guid Id; string MemberName; string MemberPhone; DateTime CheckInTime; DateTime? CheckOutTime; int? MinutesInside; CheckInMethod Method; string? DeviceName; string? Notes; }`
- Each `XxxSheet` static class exposes `public static SheetDefinition<XxxRow> Export { get; }`. `MemberSheet` and `ServicePackageSheet` additionally expose `Import` (the parse-capable subset) with `Export => Import.Plus(...)`.

**Why a dedicated row type:** see "Divergence From The Spec" at the top of this plan.

- [ ] **Step 1: Write the failing test**

Create `gym-management-server.Tests/Unit/SheetDefinitionTests.cs`:

```csharp
using gym_management_server.DTOs.Export;
using gym_management_server.Entities.Enums;
using gym_management_server.Services.Invoices;
using gym_management_server.Services.Members;
using Xunit;

namespace gym_management_server.Tests.Unit
{
    public class SheetDefinitionTests
    {
        [Fact]
        public void Member_export_columns_start_with_the_import_columns_in_the_same_order()
        {
            var import = MemberSheet.Import.Columns.Select(c => c.Header).ToList();
            var export = MemberSheet.Export.Columns.Select(c => c.Header).ToList();

            Assert.True(export.Count > import.Count);
            Assert.Equal(import, export.Take(import.Count).ToList());
        }

        [Fact]
        public void Member_export_only_columns_are_read_only()
        {
            // Read-only columns have no Parse, which is what lets an exported file be
            // edited and re-imported without first deleting the ID and date columns.
            var importCount = MemberSheet.Import.Columns.Count;
            var extras = MemberSheet.Export.Columns.Skip(importCount);

            Assert.All(extras, c => Assert.Null(c.Parse));
            Assert.Contains(MemberSheet.Export.Columns, c => c.Header == "Mã hội viên");
        }

        [Fact]
        public void Member_required_columns_are_name_and_phone()
        {
            var required = MemberSheet.Import.Columns.Where(c => c.IsRequired).Select(c => c.Header).ToList();
            Assert.Equal(new[] { "Họ và tên", "Số điện thoại" }, required);
        }

        [Fact]
        public void Enum_columns_render_the_Vietnamese_label_not_the_number()
        {
            var statusColumn = MemberSheet.Export.Columns.Single(c => c.Header == "Trạng thái");
            var row = new MemberRow { Status = MemberStatus.Suspended };

            Assert.Equal("Tạm ngưng", statusColumn.Get(row));
        }

        [Fact]
        public void Nullable_enum_columns_render_empty_rather_than_the_word_null()
        {
            var genderColumn = MemberSheet.Export.Columns.Single(c => c.Header == "Giới tính");
            Assert.Equal("", genderColumn.Get(new MemberRow { Gender = null }));
        }

        [Fact]
        public void Enum_columns_advertise_their_allowed_values_for_the_template_dropdown()
        {
            var statusColumn = MemberSheet.Import.Columns.Single(c => c.Header == "Trạng thái");
            Assert.Equal(new[] { "Hoạt động", "Tạm ngưng", "Hết hạn" }, statusColumn.AllowedValues);
        }

        [Fact]
        public void Invoice_export_shows_the_member_name_rather_than_a_raw_guid()
        {
            var headers = InvoiceSheet.Export.Columns.Select(c => c.Header).ToList();
            Assert.Contains("Hội viên", headers);
            Assert.DoesNotContain("MemberId", headers);
        }
    }
}
```

- [ ] **Step 2: Run the test and confirm it fails**

```bash
dotnet test gym-management-server/gym-management-server.sln --filter FullyQualifiedName~SheetDefinitionTests
```

Expected: compile error — `MemberRow` / `MemberSheet` do not exist.

- [ ] **Step 3: Write the row types**

Create `DTOs/Export/ExportRows.cs` holding every row class listed in **Interfaces** above. They are plain mutable data carriers, for example:

```csharp
using gym_management_server.Entities.Enums;

namespace gym_management_server.DTOs.Export
{
    public class MemberRow
    {
        public string FullName { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public string? Email { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public Gender? Gender { get; set; }
        public string? Address { get; set; }
        public string? EmergencyName { get; set; }
        public string? EmergencyPhone { get; set; }
        public MemberStatus Status { get; set; } = MemberStatus.Active;
        public string? Notes { get; set; }

        // Read-only on import.
        public Guid Id { get; set; }
        public DateTime RegistrationDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ...the remaining eight row classes, same shape
}
```

- [ ] **Step 4: Write the two importable sheet definitions**

Create `Services/Members/MemberSheet.cs`:

```csharp
using gym_management_server.DTOs.Export;
using gym_management_server.Entities.Enums;
using gym_management_server.Infrastructure.Enums;
using gym_management_server.Infrastructure.Excel;

namespace gym_management_server.Services.Members
{
    public static class MemberSheet
    {
        private static IReadOnlyList<string> Labels<TEnum>() where TEnum : struct, Enum =>
            EnumLabel.Describe<TEnum>().Select(v => v.Label).ToList();

        /// <summary>Columns a user may fill in. Parse is filled in by plan 2; the headers,
        /// order and allowed values are fixed here so export and template cannot drift.</summary>
        public static SheetDefinition<MemberRow> Import => new("Thành viên", new[]
        {
            new ExcelColumn<MemberRow>("Họ và tên", r => r.FullName, isRequired: true, width: 28),
            new ExcelColumn<MemberRow>("Số điện thoại", r => r.PhoneNumber, isRequired: true),
            new ExcelColumn<MemberRow>("Email", r => r.Email, width: 26),
            new ExcelColumn<MemberRow>("Ngày sinh", r => r.DateOfBirth, format: "dd/MM/yyyy"),
            new ExcelColumn<MemberRow>("Giới tính", r => r.Gender.ToLabel(), allowedValues: Labels<Gender>()),
            new ExcelColumn<MemberRow>("Địa chỉ", r => r.Address, width: 34),
            new ExcelColumn<MemberRow>("Người liên hệ khẩn cấp", r => r.EmergencyName, width: 24),
            new ExcelColumn<MemberRow>("SĐT khẩn cấp", r => r.EmergencyPhone),
            new ExcelColumn<MemberRow>("Trạng thái", r => r.Status.ToLabel(), allowedValues: Labels<MemberStatus>()),
            new ExcelColumn<MemberRow>("Ghi chú", r => r.Notes, width: 34),
        });

        public static SheetDefinition<MemberRow> Export => Import.Plus(
            new ExcelColumn<MemberRow>("Mã hội viên", r => r.Id, width: 38),
            new ExcelColumn<MemberRow>("Ngày đăng ký", r => r.RegistrationDate, format: "dd/MM/yyyy"),
            new ExcelColumn<MemberRow>("Ngày tạo", r => r.CreatedAt, format: "dd/MM/yyyy HH:mm"));
    }
}
```

Create `Services/ServicePackages/ServicePackageSheet.cs` following the identical shape: `Import` holds `Tên gói` (required, width 28), `Mô tả` (width 40), `Đơn giá` (format `#,##0`), `Số ngày` , `Số lượt tối đa`, `Đang áp dụng`; `Export => Import.Plus(Mã gói, Ngày tạo)`.

- [ ] **Step 5: Write the six export-only sheet definitions**

Each is a single `Export` property with no `Import`. Headers:

| File | Sheet name | Columns |
|---|---|---|
| `Services/Trainers/TrainerSheet.cs` | `Huấn luyện viên` | Mã HLV, Họ và tên, Số điện thoại, Email, Chuyên môn, Giá theo giờ (`#,##0`), Trạng thái, Ghi chú |
| `Services/Devices/AttendanceDeviceSheet.cs` | `Thiết bị điểm danh` | Mã thiết bị, Tên thiết bị, Vị trí, Hãng, Số sê-ri, Đang hoạt động, Ngày tạo (`dd/MM/yyyy HH:mm`) |
| `Services/Expenses/ExpenseSheet.cs` | `Chi phí` | Mã chi phí, Ngày chi (`dd/MM/yyyy`), Hạng mục, Diễn giải, Số tiền (`#,##0`), Hình thức, Ghi chú |
| `Services/Invoices/InvoiceSheet.cs` | `Hóa đơn` + `Chi tiết hóa đơn` | see below |
| `Services/MemberDataServices/MemberDataServiceSheet.cs` | `Đăng ký gói` | Mã đăng ký, Hội viên, Số điện thoại, Gói dịch vụ, Ngày bắt đầu, Ngày kết thúc, Giá lúc mua (`#,##0`), Lượt còn lại, Trạng thái |
| `Services/CheckIns/CheckInSheet.cs` | `Lịch sử điểm danh` | Mã phiên, Hội viên, Số điện thoại, Giờ vào (`dd/MM/yyyy HH:mm`), Giờ ra (`dd/MM/yyyy HH:mm`), Số phút, Hình thức, Thiết bị, Ghi chú |

`InvoiceSheet` carries two definitions because an invoice export ships its line items as a second sheet:

```csharp
public static SheetDefinition<InvoiceRow> Export => new("Hóa đơn", new[]
{
    new ExcelColumn<InvoiceRow>("Số hóa đơn", r => r.InvoiceNumber),
    new ExcelColumn<InvoiceRow>("Hội viên", r => r.MemberName, width: 28),
    new ExcelColumn<InvoiceRow>("Số điện thoại", r => r.MemberPhone),
    new ExcelColumn<InvoiceRow>("Ngày hóa đơn", r => r.InvoiceDate, format: "dd/MM/yyyy"),
    new ExcelColumn<InvoiceRow>("Tổng tiền", r => r.TotalAmount, format: "#,##0"),
    new ExcelColumn<InvoiceRow>("Trạng thái", r => r.Status.ToLabel()),
    new ExcelColumn<InvoiceRow>("Hình thức", r => r.PaymentMethod.ToLabel()),
    new ExcelColumn<InvoiceRow>("Ghi chú", r => r.Notes, width: 30),
    new ExcelColumn<InvoiceRow>("Mã hóa đơn", r => r.Id, width: 38),
});

public static SheetDefinition<InvoiceItemRow> Items => new("Chi tiết hóa đơn", new[]
{
    new ExcelColumn<InvoiceItemRow>("Số hóa đơn", r => r.InvoiceNumber),
    new ExcelColumn<InvoiceItemRow>("Diễn giải", r => r.Description, width: 34),
    new ExcelColumn<InvoiceItemRow>("Số lượng", r => r.Quantity),
    new ExcelColumn<InvoiceItemRow>("Đơn giá", r => r.UnitPrice, format: "#,##0"),
    new ExcelColumn<InvoiceItemRow>("Thành tiền", r => r.LineTotal, format: "#,##0"),
});
```

- [ ] **Step 6: Run the tests and confirm they pass**

```bash
dotnet test gym-management-server/gym-management-server.sln --filter FullyQualifiedName~SheetDefinitionTests
```

Expected: 7 passing.

- [ ] **Step 7: Commit**

```bash
git add gym-management-server/
git commit -m "Declare export columns for all nine modules"
```

---

### Task 6: Repository queries that feed the exports

**Files:**
- Modify: `Repositories/Members/IMemberRepository.cs.cs` + `MemberRepository.cs`, `Repositories/ServicePackages/*`, `Repositories/Trainers/*`, `Repositories/Devices/*`, `Repositories/Expenses/*`, `Repositories/Invoices/*`, `Repositories/MemberDataServices/*`, `Repositories/CheckIns/*`
- Test: `gym-management-server.Tests/Integration/ExportQueryTests.cs`

**Interfaces:**
- Consumes: the row types from Task 5.
- Produces, one method per repository, all returning materialised lists:
  - `IMemberRepository.Task<List<MemberRow>> GetForExportAsync()`
  - `IServicePackageRepository.Task<List<ServicePackageRow>> GetForExportAsync()`
  - `ITrainerRepository.Task<List<TrainerRow>> GetForExportAsync()`
  - `IAttendanceDeviceRepository.Task<List<AttendanceDeviceRow>> GetForExportAsync()`
  - `IExpenseRepository.Task<List<ExpenseRow>> GetForExportAsync(DateTime? from, DateTime? to)`
  - `IInvoiceRepository.Task<(List<InvoiceRow> Invoices, List<InvoiceItemRow> Items)> GetForExportAsync(DateTime? from, DateTime? to)`
  - `IMemberDataServiceRepository.Task<List<SubscriptionRow>> GetForExportAsync(DateTime? from, DateTime? to)`
  - `ICheckInRepository.Task<List<AttendanceRow>> GetForExportAsync(DateTime? from, DateTime? to)`

**Date filter semantics (apply identically everywhere):** `from` is inclusive, `to` is **exclusive at the end of that day** — the caller passes a date, and the query uses `< to.Value.Date.AddDays(1)` so that picking 30/09 includes everything that happened on 30/09. Either bound may be null.

- [ ] **Step 1: Write the failing test**

Create `gym-management-server.Tests/Integration/ExportQueryTests.cs`:

```csharp
using gym_management_server.Data.EntityFramework;
using gym_management_server.Entities.Enums;
using gym_management_server.Entities.Invoices;
using gym_management_server.Entities.InvoiceItems;
using gym_management_server.Entities.Members;
using gym_management_server.Repositories.Invoices;
using gym_management_server.Repositories.Members;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace gym_management_server.Tests.Integration
{
    public class ExportQueryTests
    {
        private static GymManagementContext NewContext() =>
            new(new DbContextOptionsBuilder<GymManagementContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        [Fact]
        public async Task Member_export_skips_soft_deleted_rows()
        {
            using var db = NewContext();
            db.Members.Add(new Member { Id = Guid.NewGuid(), FullName = "Còn", PhoneNumber = "0900000001" });
            db.Members.Add(new Member { Id = Guid.NewGuid(), FullName = "Đã xóa", PhoneNumber = "0900000002", IsDeleted = true });
            await db.SaveChangesAsync();

            var rows = await new MemberRepository(db).GetForExportAsync();

            Assert.Single(rows);
            Assert.Equal("Còn", rows[0].FullName);
        }

        [Fact]
        public async Task Invoice_export_resolves_the_member_name_and_returns_its_line_items()
        {
            using var db = NewContext();
            var member = new Member { Id = Guid.NewGuid(), FullName = "Nguyễn Văn A", PhoneNumber = "0900000003" };
            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                InvoiceNumber = "HD001",
                MemberId = member.Id,
                InvoiceDate = new DateTime(2026, 5, 10),
                TotalAmount = 1_200_000m,
                Status = InvoiceStatus.Paid,
            };
            invoice.Items.Add(new InvoiceItem { Id = Guid.NewGuid(), InvoiceId = invoice.Id, Description = "Gói 3 tháng", Quantity = 2, UnitPrice = 600_000m });
            db.Members.Add(member);
            db.Invoices.Add(invoice);
            await db.SaveChangesAsync();

            var (invoices, items) = await new InvoiceRepository(db).GetForExportAsync(null, null);

            Assert.Equal("Nguyễn Văn A", invoices[0].MemberName);
            Assert.Equal("HD001", items[0].InvoiceNumber);
            Assert.Equal(1_200_000m, items[0].LineTotal);   // Quantity * UnitPrice
        }

        [Fact]
        public async Task The_to_bound_includes_everything_that_happened_on_that_day()
        {
            using var db = NewContext();
            db.Invoices.Add(new Invoice { Id = Guid.NewGuid(), InvoiceNumber = "HD002", InvoiceDate = new DateTime(2026, 5, 31, 23, 30, 0) });
            await db.SaveChangesAsync();

            var (invoices, _) = await new InvoiceRepository(db)
                .GetForExportAsync(new DateTime(2026, 5, 1), new DateTime(2026, 5, 31));

            Assert.Single(invoices);   // 23:30 on the last day must still be inside the range
        }
    }
}
```

- [ ] **Step 2: Run the test and confirm it fails**

```bash
dotnet test gym-management-server/gym-management-server.sln --filter FullyQualifiedName~ExportQueryTests
```

Expected: compile error — `GetForExportAsync` is not on the repositories.

- [ ] **Step 3: Implement the simple repositories**

Add to `IMemberRepository` and implement in `MemberRepository`:

```csharp
public async Task<List<MemberRow>> GetForExportAsync() =>
    await _db.Members
        .Where(x => !x.IsDeleted)
        .OrderBy(x => x.FullName)
        .Select(x => new MemberRow
        {
            Id = x.Id,
            FullName = x.FullName,
            PhoneNumber = x.PhoneNumber,
            Email = x.Email,
            DateOfBirth = x.DateOfBirth,
            Gender = x.Gender,
            Address = x.Address,
            EmergencyName = x.EmergencyName,
            EmergencyPhone = x.EmergencyPhone,
            Status = x.Status,
            Notes = x.Notes,
            RegistrationDate = x.RegistrationDate,
            CreatedAt = x.CreatedAt,
        })
        .ToListAsync();
```

Do the same for `ServicePackageRepository`, `TrainerRepository` and `AttendanceDeviceRepository` (the last one filtering `!x.IsDeleted`), projecting straight into their row type.

- [ ] **Step 4: Implement the four filtered repositories**

`InvoiceRepository` — note the join is what makes the member's name available, which the CRUD DTO cannot supply:

```csharp
public async Task<(List<InvoiceRow> Invoices, List<InvoiceItemRow> Items)> GetForExportAsync(DateTime? from, DateTime? to)
{
    var query = _db.Invoices.AsQueryable();
    if (from.HasValue) query = query.Where(x => x.InvoiceDate >= from.Value.Date);
    // `to` is inclusive of the whole day the caller picked.
    if (to.HasValue) query = query.Where(x => x.InvoiceDate < to.Value.Date.AddDays(1));

    var invoices = await query
        .OrderByDescending(x => x.InvoiceDate)
        .Select(x => new InvoiceRow
        {
            Id = x.Id,
            InvoiceNumber = x.InvoiceNumber,
            MemberName = x.Member != null ? x.Member.FullName : null,
            MemberPhone = x.Member != null ? x.Member.PhoneNumber : null,
            InvoiceDate = x.InvoiceDate,
            TotalAmount = x.TotalAmount,
            Status = x.Status,
            PaymentMethod = x.PaymentMethod,
            Notes = x.Notes,
        })
        .ToListAsync();

    var numbers = invoices.Select(i => i.InvoiceNumber).ToList();
    var items = await _db.InvoiceItems
        .Where(i => numbers.Contains(i.Invoice.InvoiceNumber))
        .Select(i => new InvoiceItemRow
        {
            InvoiceNumber = i.Invoice.InvoiceNumber,
            Description = i.Description,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            LineTotal = i.Quantity * i.UnitPrice,
        })
        .ToListAsync();

    return (invoices, items);
}
```

`ExpenseRepository` filters on `ExpenseDate`, `MemberDataServiceRepository` on `StartDate` (joining `Member` and `ServicePackage` for their names), `CheckInRepository` on `CheckInTime` (joining `Member` and `Device`, and computing `MinutesInside` from `CheckOutTime - CheckInTime` where the session is closed). Use the same `from`/`to` expressions as above in each.

- [ ] **Step 5: Run the tests and confirm they pass**

```bash
dotnet test gym-management-server/gym-management-server.sln --filter FullyQualifiedName~ExportQueryTests
```

Expected: 3 passing.

- [ ] **Step 6: Commit**

```bash
git add gym-management-server/
git commit -m "Add export queries that resolve related names and filter by date"
```

---

### Task 7: The export endpoints

**Files:**
- Modify: all nine controllers under `gym-management-server/gym-management-server/Controllers/`
- Create: `Infrastructure/Excel/ExcelFileResult.cs`
- Modify: `Services/` — one `ExportAsync` method per service, delegating to its repository and sheet
- Test: `gym-management-server.Tests/Integration/ExportApiTests.cs`

**Interfaces:**
- Consumes: `ExcelWriter` (Task 4), the sheets (Task 5), the repository queries (Task 6).
- Produces: `GET /api/{Member|ServicePackage|Trainer|AttendanceDevice}/export` and `GET /api/{MemberDataService|CheckIn|Invoice|Expense}/export?from=&to=`. Helper `ExcelFileResult.File(byte[] bytes, string baseName)` returning a `FileContentResult` named `{baseName}-{yyyy-MM-dd}.xlsx` with `ExcelWriter.ContentType`.

**Authorisation — copy exactly, do not improvise:**

| Controller | `[Authorize]` on export |
|---|---|
| `Member`, `ServicePackage`, `Trainer`, `MemberDataService`, `CheckIn` | `[Authorize]` |
| `Attendance` | inherits the controller's existing `[Authorize(Roles = "0,1")]` |
| `AttendanceDevice` | inherits the controller's existing `[Authorize(Roles = "1")]` |
| `Invoice`, `Expense` | `[Authorize(Roles = "1")]` — financial data is Admin-only |

- [ ] **Step 1: Write the failing test**

Create `gym-management-server.Tests/Integration/ExportApiTests.cs`:

```csharp
using System.IO;
using System.Net;
using ClosedXML.Excel;
using gym_management_server.Data.EntityFramework;
using gym_management_server.Entities.Members;
using gym_management_server.Infrastructure.Excel;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace gym_management_server.Tests.Integration
{
    public class ExportApiTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        public ExportApiTests(CustomWebApplicationFactory factory) => _factory = factory;

        private void Seed(Action<GymManagementContext> seed)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GymManagementContext>();
            seed(db);
            db.SaveChanges();
        }

        [Fact]
        public async Task Export_requires_authentication()
        {
            var response = await _factory.CreateClient().GetAsync("/api/Member/export");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Staff_may_not_export_financial_data()
        {
            var staff = _factory.CreateAuthenticatedClient(role: 0);

            Assert.Equal(HttpStatusCode.Forbidden, (await staff.GetAsync("/api/Invoice/export")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await staff.GetAsync("/api/Expense/export")).StatusCode);
        }

        [Fact]
        public async Task Export_returns_an_xlsx_with_a_header_row_and_one_row_per_member()
        {
            Seed(db =>
            {
                db.Members.Add(new Member { Id = Guid.NewGuid(), FullName = "Nguyễn Văn A", PhoneNumber = "0911000001" });
                db.Members.Add(new Member { Id = Guid.NewGuid(), FullName = "Trần Thị B", PhoneNumber = "0911000002" });
            });

            var response = await _factory.CreateAuthenticatedClient(role: 0).GetAsync("/api/Member/export");
            response.EnsureSuccessStatusCode();

            Assert.Equal(ExcelWriter.ContentType, response.Content.Headers.ContentType?.MediaType);
            Assert.Contains("thanh-vien", response.Content.Headers.ContentDisposition!.FileNameStar
                ?? response.Content.Headers.ContentDisposition!.FileName!);

            using var wb = new XLWorkbook(new MemoryStream(await response.Content.ReadAsByteArrayAsync()));
            var ws = wb.Worksheet("Thành viên");
            Assert.Equal("Họ và tên", ws.Cell(1, 1).GetString());
            Assert.Equal(3, ws.LastRowUsed()!.RowNumber());   // header + 2 members
        }

        [Fact]
        public async Task Invoice_export_carries_its_line_items_on_a_second_sheet()
        {
            var admin = _factory.CreateAuthenticatedClient(role: 1);
            var response = await admin.GetAsync("/api/Invoice/export");
            response.EnsureSuccessStatusCode();

            using var wb = new XLWorkbook(new MemoryStream(await response.Content.ReadAsByteArrayAsync()));
            Assert.True(wb.Worksheets.Contains("Hóa đơn"));
            Assert.True(wb.Worksheets.Contains("Chi tiết hóa đơn"));
        }

        [Fact]
        public async Task Export_over_the_row_limit_is_rejected_with_400()
        {
            Seed(db =>
            {
                for (var i = 0; i <= ExcelWriter.MaxRows; i++)
                    db.Members.Add(new Member { Id = Guid.NewGuid(), FullName = $"HV{i}", PhoneNumber = $"09{i:D8}" });
            });

            var response = await _factory.CreateAuthenticatedClient(role: 0).GetAsync("/api/Member/export");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("vượt giới hạn", await response.Content.ReadAsStringAsync());
        }
    }
}
```

Note: the row-limit test seeds 50,001 rows into the in-memory provider. If it runs slower than ~30s on the target machine, mark it `[Fact(Skip = "slow")]` and cover the limit through `ExcelWriterTests` alone — but try it first.

- [ ] **Step 2: Run the test and confirm it fails**

```bash
dotnet test gym-management-server/gym-management-server.sln --filter FullyQualifiedName~ExportApiTests
```

Expected: 404s — no export endpoints yet.

- [ ] **Step 3: Write the file-result helper**

Create `Infrastructure/Excel/ExcelFileResult.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;

namespace gym_management_server.Infrastructure.Excel
{
    public static class ExcelFileResult
    {
        public static FileContentResult File(byte[] bytes, string baseName) =>
            new(bytes, ExcelWriter.ContentType)
            {
                FileDownloadName = $"{baseName}-{DateTime.UtcNow.AddHours(7):yyyy-MM-dd}.xlsx"
            };
    }
}
```

- [ ] **Step 4: Add an ExportAsync to each service**

For example in `Services/Members/MemberService.cs`:

```csharp
public async Task<byte[]> ExportAsync() =>
    ExcelWriter.Write(MemberSheet.Export, await _memberRepository.GetForExportAsync());
```

`InvoiceService` writes both sheets:

```csharp
public async Task<byte[]> ExportAsync(DateTime? from, DateTime? to)
{
    var (invoices, items) = await _invoiceRepository.GetForExportAsync(from, to);
    return ExcelWriter.Write(
        new SheetPayload<InvoiceRow>(InvoiceSheet.Export, invoices),
        new SheetPayload<InvoiceItemRow>(InvoiceSheet.Items, items));
}
```

- [ ] **Step 5: Add the endpoints**

In `MemberController`:

```csharp
[HttpGet("export")]
[Authorize]
public async Task<IActionResult> Export()
{
    try
    {
        return ExcelFileResult.File(await _service.ExportAsync(), "thanh-vien");
    }
    catch (ExcelRowLimitExceededException ex)
    {
        return BadRequest(new { message = ex.Message });
    }
}
```

File base names, one per controller: `thanh-vien`, `goi-dich-vu`, `huan-luyen-vien`, `thiet-bi-diem-danh`, `dang-ky-goi`, `check-in`, `lich-su-diem-danh`, `hoa-don`, `chi-phi`.

The four filtered controllers take `[FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null` and pass them through. Apply the `[Authorize]` attribute from the table above to each.

Add `using gym_management_server.Infrastructure.Excel;` and `using Microsoft.AspNetCore.Authorization;` to any controller that lacks them.

- [ ] **Step 6: Run the whole suite**

```bash
dotnet test gym-management-server/gym-management-server.sln
```

Expected: everything passes.

- [ ] **Step 7: Commit**

```bash
git add gym-management-server/
git commit -m "Add Excel export endpoints to all nine modules"
```

---

### Task 8: The export button in the UI

**Files:**
- Create: `gym-management-client/src/app/shared/services/file-download.service.ts`
- Create: `gym-management-client/src/app/shared/components/export-button/export-button.component.ts`, `.html`, `.scss`
- Modify: `gym-management-client/src/app/app.module.ts` (declare and export the component)
- Modify: the nine grid component templates under `gym-management-client/src/app/modules/*/components/*/*.component.html`
- Test: `gym-management-client/src/app/shared/services/file-download.service.spec.ts`

**Interfaces:**
- Consumes: the export endpoints from Task 7.
- Produces: `FileDownloadService.download(url: string, params?: Record<string, string>): Observable<void>` — issues a `responseType: 'blob'` GET, reads the filename from `Content-Disposition` (falling back to a generated one), and triggers the browser download. `<app-export-button [url]="'/api/Member/export'" [fallbackName]="'thanh-vien.xlsx'">`.

- [ ] **Step 1: Write the failing test**

Create `gym-management-client/src/app/shared/services/file-download.service.spec.ts`:

```typescript
import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { FileDownloadService } from './file-download.service';

describe('FileDownloadService', () => {
    let service: FileDownloadService;
    let http: HttpTestingController;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [FileDownloadService, provideHttpClient(), provideHttpClientTesting()],
        });
        service = TestBed.inject(FileDownloadService);
        http = TestBed.inject(HttpTestingController);
    });

    afterEach(() => http.verify());

    it('requests the url as a blob', () => {
        service.download('/api/Member/export', 'thanh-vien.xlsx').subscribe();

        const req = http.expectOne(r => r.url === '/api/Member/export');
        expect(req.request.responseType).toBe('blob');
        req.flush(new Blob(['x']), {
            headers: { 'content-disposition': 'attachment; filename=thanh-vien-2026-09-22.xlsx' },
        });
    });

    it('prefers the filename from content-disposition', () => {
        let used = '';
        service.saveAs = (_blob: Blob, name: string) => { used = name; };

        service.download('/api/Member/export', 'fallback.xlsx').subscribe();
        http.expectOne('/api/Member/export').flush(new Blob(['x']), {
            headers: { 'content-disposition': 'attachment; filename=thanh-vien-2026-09-22.xlsx' },
        });

        expect(used).toBe('thanh-vien-2026-09-22.xlsx');
    });

    it('falls back when the header is absent', () => {
        let used = '';
        service.saveAs = (_blob: Blob, name: string) => { used = name; };

        service.download('/api/Member/export', 'fallback.xlsx').subscribe();
        http.expectOne('/api/Member/export').flush(new Blob(['x']));

        expect(used).toBe('fallback.xlsx');
    });
});
```

- [ ] **Step 2: Run the test and confirm it fails**

```bash
cd gym-management-client && npm test -- --watch=false --browsers=ChromeHeadless
```

Expected: cannot resolve `./file-download.service`.

- [ ] **Step 3: Write the service**

Create `gym-management-client/src/app/shared/services/file-download.service.ts`:

```typescript
import { Injectable } from '@angular/core';
import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { Observable, map } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class FileDownloadService {
    constructor(private http: HttpClient) { }

    /** Overridable so tests can assert the filename without touching the DOM. */
    saveAs(blob: Blob, filename: string): void {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = filename;
        link.click();
        URL.revokeObjectURL(url);
    }

    download(url: string, fallbackName: string, params?: Record<string, string>): Observable<void> {
        return this.http
            .get(url, {
                params: new HttpParams({ fromObject: params ?? {} }),
                responseType: 'blob',
                observe: 'response',
            })
            .pipe(
                map((response: HttpResponse<Blob>) => {
                    this.saveAs(response.body!, this.filenameFrom(response) ?? fallbackName);
                })
            );
    }

    private filenameFrom(response: HttpResponse<Blob>): string | null {
        const header = response.headers.get('content-disposition');
        if (!header) return null;
        // Handles both `filename=x.xlsx` and RFC 5987 `filename*=UTF-8''x.xlsx`.
        const match = /filename\*?=(?:UTF-8'')?"?([^";]+)"?/i.exec(header);
        return match ? decodeURIComponent(match[1]) : null;
    }
}
```

- [ ] **Step 4: Run the test and confirm it passes**

```bash
cd gym-management-client && npm test -- --watch=false --browsers=ChromeHeadless
```

Expected: 3 passing.

- [ ] **Step 5: Write the button component**

Create `gym-management-client/src/app/shared/components/export-button/export-button.component.ts`:

```typescript
import { Component, Input } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { FileDownloadService } from '../../services/file-download.service';

@Component({
    selector: 'app-export-button',
    standalone: false,
    templateUrl: './export-button.component.html',
    styleUrls: ['./export-button.component.scss'],
})
export class ExportButtonComponent {
    @Input({ required: true }) url!: string;
    @Input({ required: true }) fallbackName!: string;
    /** Optional date range forwarded as ?from=&to= */
    @Input() params?: Record<string, string>;

    busy = false;

    constructor(private downloads: FileDownloadService, private snackBar: MatSnackBar) { }

    export(): void {
        this.busy = true;
        this.downloads.download(this.url, this.fallbackName, this.params).subscribe({
            next: () => { this.busy = false; },
            error: (err) => {
                this.busy = false;
                const message = err.status === 403
                    ? 'Bạn không có quyền xuất dữ liệu này.'
                    : err.error?.message ?? 'Xuất Excel thất bại.';
                this.snackBar.open(message, 'OK', { duration: 5000 });
            },
        });
    }
}
```

`export-button.component.html`:

```html
<button mat-stroked-button color="primary" [disabled]="busy" (click)="export()">
  <mat-icon>file_download</mat-icon>
  {{ busy ? 'Đang xuất...' : 'Xuất Excel' }}
</button>
```

`export-button.component.scss`: leave empty.

- [ ] **Step 6: Declare the component**

In `gym-management-client/src/app/app.module.ts`, add `ExportButtonComponent` to `declarations` and to `exports` (the lazy feature modules import the shared module to use it — follow whatever the existing `SidebarComponent` / `LoadingComponent` do in this file and mirror it exactly).

- [ ] **Step 7: Place the button in all nine grids**

In each grid template, add the button above the `<dx-data-grid>`:

```html
<div class="grid-toolbar">
  <app-export-button [url]="'/api/Member/export'" [fallbackName]="'thanh-vien.xlsx'"></app-export-button>
</div>
```

URL and fallback per grid: `/api/Member/export` + `thanh-vien.xlsx`, `/api/ServicePackage/export` + `goi-dich-vu.xlsx`, `/api/Trainer/export` + `huan-luyen-vien.xlsx`, `/api/AttendanceDevice/export` + `thiet-bi-diem-danh.xlsx`, `/api/MemberDataService/export` + `dang-ky-goi.xlsx`, `/api/CheckIn/export` + `check-in.xlsx`, `/api/Invoice/export` + `hoa-don.xlsx`, `/api/Expense/export` + `chi-phi.xlsx`, `/api/Attendance/export` + `lich-su-diem-danh.xlsx`.

The `invoice-items` grid does **not** get a button — its rows ship inside the invoice export.

- [ ] **Step 8: Build and verify by hand**

```bash
cd gym-management-client && npm run build
```

Then run both halves and click one button end to end:

```bash
dotnet run --project gym-management-server/gym-management-server
```

```bash
cd gym-management-client && npm start
```

Log in, open Thành viên, click **Xuất Excel**, open the downloaded file. Confirm the header row is Vietnamese, the status column reads "Hoạt động" rather than `0`, and the row count matches the grid's total.

- [ ] **Step 9: Commit**

```bash
git add gym-management-client/
git commit -m "Add an Excel export button to every grid"
```

---

## Self-Review

**Spec coverage**

| Spec section | Task |
|---|---|
| 3.2 enums + defaults | 1, 2 |
| 3.3 single label source (`/api/Enums`, Excel, template) | 1, 3; template dropdown values carried on `AllowedValues` in Task 5, consumed by plan 2 |
| 3.4 SQL script, not auto-run | 2 step 7 |
| 4.1 Excel infrastructure | 4 (writer side); `ExcelReader` / `RowError` / `ReadResult` belong to plan 2 |
| 4.2 declaration drives export + template + parser | 4, 5 |
| 4.3 50,000 row cap | 4, 7 |
| 5.1 nine export endpoints, `InvoiceItem` as a second sheet, no fingerprint export | 5, 7 |
| 5.2 full set, not a page; `GetForExportAsync` | 6 |
| 5.3 `ExportButtonComponent` + `FileDownloadService` | 8 |
| 8 authorisation, export matches GET | 7 |
| 9 unit + integration tests | every task |

Sections 6 (import) and 7 (dashboard) are deliberately out of scope — see the companion plans.

**Gap found and closed:** the spec assumes an authenticated test client, but `CustomWebApplicationFactory` had no way to issue one. Added as Task 3 step 2, before the first test that needs it.

**Type consistency:** `GetForExportAsync` is spelled identically in Tasks 6 and 7. `ExcelWriter.Write` has the two overloads used in Task 7. `ExcelColumn<T>`'s `Parse` and `AllowedValues` are declared in Task 4 and populated in Task 5, and plan 2 consumes both without changing the type. `MemberSheet.Import` / `.Export` are referenced consistently in Tasks 5 and 7. `FileDownloadService.download` takes `(url, fallbackName, params?)` in both its test and its call site.
