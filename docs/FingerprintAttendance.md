# Fingerprint Attendance — Implementation Documentation

Allows gym members to check in and check out by scanning a fingerprint. After a successful
match the system identifies the member and toggles their attendance: a check-in if no session
is open, otherwise a check-out. The design is **vendor-neutral** — any scanner that produces a
template works, and ZKTeco / Suprema / DigitalPersona can be plugged in without touching
business logic.

---

## 1. Architecture

The feature follows the project's existing layering: **Entity → DTO → Repository → Service → Controller**,
with `AddScoped` DI and the reflection-based `GymManagementServiceMapObjects` mapper.

A new **hardware-abstraction layer** keeps business logic independent of any device vendor:

```
                       FingerprintService  (business logic)
                                │ depends only on the interfaces below
        ┌───────────────────────┼─────────────────────────┐
        ▼                        ▼                         ▼
IFingerprintProviderFactory   ITemplateProtector     Repositories (EF Core)
        │                        │
        ▼                        ▼
 resolves by Vendor       AesTemplateProtector (AES-256-GCM)
        │
        ▼
 IFingerprintProvider  ── Mock | ZKTeco | Suprema | DigitalPersona
```

- **`IFingerprintProvider`** — `CreateTemplate` (normalise a captured template) and
  `Identify` (match a probe against candidates). One implementation per vendor.
- **`FingerprintProviderFactory`** — indexes all registered providers by their `Vendor` key and
  resolves the right one for a device. Adding a vendor is a **one-line DI registration**; no
  business-logic edits.
- **`ITemplateProtector` / `AesTemplateProtector`** — encrypts template bytes at rest with
  AES-256-GCM; templates are only ever decrypted transiently in memory during matching.

Matching runs **server-side** against stored templates, so the server stays vendor-neutral while
device SDKs only handle capture → template at the edge.

### Key files

| Layer | Path |
|---|---|
| Entities | `Entities/Fingerprints/FingerprintTemplate.cs`, `Entities/Devices/AttendanceDevice.cs`, `Entities/CheckIns/CheckIn.cs` (evolved) |
| Abstraction | `Fingerprints/IFingerprintProvider.cs`, `FingerprintProviderFactory.cs`, `ITemplateProtector.cs`, `AesTemplateProtector.cs`, `Fingerprints/Providers/*` |
| DTOs | `DTOs/Fingerprints/*`, `DTOs/Devices/*`, `DTOs/CheckIns/AttendanceOutput.cs` |
| Repositories | `Repositories/Fingerprints/*`, `Repositories/Devices/*`, `Repositories/CheckIns/*` (extended) |
| Services | `Services/Fingerprints/FingerprintService.cs`, `Services/Devices/AttendanceDeviceService.cs` |
| Controllers | `Controllers/FingerprintController.cs`, `AttendanceController.cs`, `AttendanceDeviceController.cs` |
| Migration | `Migrations/*_AddFingerprintAttendance.cs` |
| Tests | `gym-management-server.Tests/Unit/*`, `gym-management-server.Tests/Integration/*` |
| Frontend | `modules/fingerprint-attendance/*`, `modules/attendance-devices/*` |

---

## 2. Database design

### `FingerprintTemplate`
| Column | Type | Notes |
|---|---|---|
| Id | uniqueidentifier | PK |
| MemberId | uniqueidentifier | FK → Members (cascade), **indexed** |
| FingerPosition | tinyint | which finger (0–9) |
| Template | varbinary(max) | **AES-GCM encrypted** template — never an image |
| Vendor | nvarchar(50) | template format, selects provider |
| Quality | tinyint | enrolment quality (0–100) |
| CreatedAt / UpdatedAt / CreatedBy | audit | `CreatedBy` = enrolling user |
| IsDeleted | bit | **soft delete** |
| RowVersion | rowversion | **optimistic locking** |

Indexes: `IX_FingerprintTemplates_MemberId`; unique `(MemberId, FingerPosition)` **filtered** on
`IsDeleted = 0` (one active template per finger, soft-deleted rows don't collide).

### `AttendanceDevice`
| Column | Type | Notes |
|---|---|---|
| Id | uniqueidentifier | PK |
| Name | nvarchar(150) | required |
| Location | nvarchar(200) | |
| Vendor | nvarchar(50) | drives provider factory |
| SerialNumber | nvarchar(100) | unique (filtered, NULLs allowed) |
| IsActive | bit | inactive devices reject scans |
| CreatedAt / UpdatedAt / CreatedBy | audit | |
| IsDeleted | bit | soft delete |
| RowVersion | rowversion | optimistic locking |

### `CheckIn` (evolved into an attendance session)
Existing columns kept; **added**:
| Column | Type | Notes |
|---|---|---|
| CheckOutTime | datetime2 null | null = session still open ("active attendance") |
| DeviceId | uniqueidentifier null | FK → AttendanceDevices (set-null on delete) |
| OperatorUserId | uniqueidentifier null | staff who operated the device, if any |

`Method` now uses `CheckInMethod` constants (`Unknown=0`, `Card=1`, `Fingerprint=2`).
Index `(MemberId, CheckOutTime)` makes "find the open session" fast.

---

## 3. Verify sequence (scan → attendance)

```
Device → POST /api/Fingerprint/verify { deviceId, capturedTemplate, operatorUserId? }
  │
  ├─ load device; reject if missing/inactive (→ 400)
  ├─ provider = factory.GetProvider(device.Vendor)
  ├─ candidates = all active templates, decrypted in memory
  ├─ match = provider.Identify(probe, candidates)
  │
  ├─ if !matched  → 200 { matched:false, action:"none", score }
  └─ else:
       active = CheckIns where MemberId == match && CheckOutTime == null
       ├─ active == null → INSERT CheckIn(now, Fingerprint, deviceId, operator)  → action:"check-in"
       └─ active != null → SET active.CheckOutTime = now                          → action:"check-out"
       200 { matched:true, memberId, memberName, action, checkInId, timestamp, score }
```

Every record captures **timestamp, device ID, and operator** (when supplied).

---

## 4. API reference

| Method | Route | Auth | Purpose |
|---|---|---|---|
| GET | `/api/Fingerprint/member/{memberId}` | Staff/Admin | List a member's fingerprints (no template bytes) |
| POST | `/api/Fingerprint` | Staff/Admin | Register (enrol) a template |
| PUT | `/api/Fingerprint/{id}` | Staff/Admin | Update / re-enrol a template |
| DELETE | `/api/Fingerprint/{id}` | Staff/Admin | Soft-delete a template |
| POST | `/api/Fingerprint/verify` | Device API key | Match → check-in / check-out |
| GET | `/api/Attendance/member/{memberId}` | Staff/Admin | Attendance history (paged) |
| GET | `/api/Attendance/active` | Staff/Admin | Members currently checked in |
| GET | `/api/Attendance/summary?date=` | Staff/Admin | Day counts: check-ins / check-outs / inside |
| GET | `/api/Attendance/recent?count=` | Staff/Admin | Recent check-in/out events |
| GET/POST/PUT/DELETE | `/api/AttendanceDevice` | **Admin** | Device management |

Roles are numeric JWT claims: `0` = Staff, `1` = Admin. See `Fingerprint.http` for runnable examples.

### Representative payloads

Enrol:
```json
POST /api/Fingerprint
{ "memberId": "...", "fingerPosition": 1, "capturedTemplate": "<base64>", "vendor": "Mock", "quality": 90 }
```

Verify result:
```json
{ "matched": true, "score": 100, "memberId": "...", "memberName": "Nguyen Van A",
  "action": "check-in", "checkInId": "...", "timestamp": "2026-06-27T08:00:00Z" }
```

---

## 5. Security

- **No raw images.** Only vendor templates are accepted (base64) and stored. The API never
  returns template bytes — `FingerprintTemplateOutput` deliberately omits them.
- **Encryption at rest.** Templates are AES-256-GCM encrypted via `AesTemplateProtector`. The
  256-bit key is supplied as base64 in configuration `Fingerprint:EncryptionKey`
  (**store in user-secrets / environment for production**, not in `appsettings.json`).
- **Authenticated overwrite/auth.** Enrolment and history require Staff/Admin; device management
  is Admin-only. The enrolling user is recorded in `CreatedBy`.
- **Device endpoint.** `verify` is not behind user JWT (it is called by hardware), but it is
  guarded by a **shared device API key**: send `X-Device-Api-Key` matching
  `Fingerprint:DeviceApiKey`. The check uses a fixed-time comparison. If the config value is empty
  the check is disabled (development / tests). For stronger isolation, issue a per-device key or
  use mTLS.
- **Optimistic locking** via `RowVersion` guards against concurrent template/device edits.

---

## 6. Frontend

Three lazy-loaded Angular modules, registered in `app-routing.module.ts` and the sidebar:

- **Tổng quan điểm danh** (`/attendance-dashboard`) — KPI cards (currently inside, check-ins/-outs
  today), a "who's inside now" grid, and a recent-activity feed. Polls the `active` / `summary` /
  `recent` endpoints every 15s (with a manual refresh button); polling stops on navigate-away.
- **Điểm danh vân tay** (`/fingerprint-attendance`) — one screen, two panels:
  - *Verify / kiosk*: pick a device, submit a scan, see a colored check-in/check-out card.
  - *Enrolment*: pick a member, enrol a finger, list/delete their templates.
  - With no hardware, a "scan" is a **seed phrase** deterministically expanded to a 32-byte base64
    template; enrolling and scanning the same seed matches via the Mock provider, so the full loop
    is demoable in the browser.
- **Thiết bị điểm danh** (`/attendance-devices`) — DevExtreme CRUD grid for devices.

---

## 7. Adding a real vendor (ZKTeco / Suprema / DigitalPersona)

1. Implement `CreateTemplate` and `Identify` in the vendor's provider class under
   `Fingerprints/Providers/` (the stub already has the right `Vendor` key and signatures).
2. Add the vendor SDK NuGet/native dependency to the server project.
3. The provider is already registered in `Program.cs`; if you add a brand-new vendor, register it:
   `builder.Services.AddSingleton<IFingerprintProvider, YourProvider>();`
4. Create an `AttendanceDevice` with `Vendor = "<YourVendor>"`; the factory routes to it
   automatically. **No changes to `FingerprintService` or controllers are needed.**

For production, also: authenticate the `verify` endpoint per device (API key / mTLS), and move the
encryption key to a secret store / KMS.

---

## 8. Build, migrate, test

```bash
# Backend build
dotnet build gym-management-server/gym-management-server/gym-management-server.csproj

# Apply the schema (requires a reachable SQL Server in ConnectionStrings:Default)
dotnet ef database update --project gym-management-server/gym-management-server/gym-management-server.csproj

# Run tests (unit + integration, EF InMemory — no SQL Server needed)
dotnet test gym-management-server/gym-management-server.Tests/gym-management-server.Tests.csproj

# Frontend build
cd gym-management-client && npx ng build --configuration development
```

Configuration:
- `Fingerprint:EncryptionKey` (**required**) — base64-encoded 32-byte AES key, e.g.
  `dotnet user-secrets set "Fingerprint:EncryptionKey" "<base64-32-bytes>"`.
- `Fingerprint:DeviceApiKey` (optional) — shared key required in the `X-Device-Api-Key` header on
  `/api/Fingerprint/verify`. Leave empty to disable the device-auth check (development / tests).

> The test project targets **net8.0** to match the app's framework (avoids a
> `System.Text.Json` / `PipeWriter` mismatch when hosting under a newer SDK).
