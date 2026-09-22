# Thiết kế: Export/Import Excel và Dashboard tổng quan

- **Ngày:** 2026-09-22
- **Trạng thái:** Đã duyệt, chờ lập kế hoạch thực thi
- **Phạm vi:** `gym-management-server` (ASP.NET Core 8) và `gym-management-client` (Angular 20)

## 1. Mục tiêu

Bổ sung hai năng lực cho hệ thống quản lý phòng gym:

1. **Export Excel** cho toàn bộ 9 module dữ liệu hiện có.
2. **Import Excel** cho Thành viên và Gói dịch vụ — hai tập dữ liệu thực tế cần nhập hàng loạt khi khởi tạo hệ thống hoặc chuyển từ file Excel cũ sang.
3. **Dashboard tổng quan kinh doanh** làm trang chủ, thay cho việc mở thẳng vào danh sách thành viên.

Kèm theo là một việc nền bắt buộc: **chuẩn hóa enum trạng thái**. Đây không phải mở rộng phạm vi tùy hứng mà là điều kiện cần — chi tiết ở mục 3.

## 2. Bối cảnh

Hệ thống hiện có 9 module CRUD (thành viên, gói dịch vụ, đăng ký gói, hóa đơn, chi tiết hóa đơn, chi phí, huấn luyện viên, check-in, điểm danh vân tay) theo pattern Entity → DTO → Repository → Service → Controller. Frontend dùng DevExtreme DataGrid trong các NgModule lazy-load.

Hai quan sát định hình thiết kế này:

- **Repository trả về `List<T>` đã materialize, không lộ `IQueryable`.** Thiết kế dưới đây tôn trọng ranh giới đó thay vì phá nó để tiện tổng hợp dữ liệu.
- **`chart.js` và `ng2-charts` đã nằm trong `package.json` nhưng chưa được dùng ở đâu.** Dashboard sẽ dùng chúng, không thêm thư viện biểu đồ mới.

## 3. Việc nền bắt buộc: chuẩn hóa enum

### 3.1 Vấn đề

Nhãn trạng thái đang được khai báo lặp lại trong từng component Angular, và giá trị mặc định trong entity C# **không khớp** với nhãn đó:

| Entity | Default trong C# | Nhãn tương ứng trên UI | Hậu quả |
|---|---|---|---|
| `Invoice.Status` | `1` | Đã thanh toán | Mọi hóa đơn mới đều được tính là đã thu tiền |
| `MemberDataService.Status` | `1` | Hết hạn | Mọi đăng ký gói mới đều sinh ra ở trạng thái hết hạn |
| `Member.Status` | `1` | Tạm ngưng | Mọi hội viên mới đều là tạm ngưng |
| `Trainer.Status` | `0` | Đang làm việc | Đúng |

Dashboard đọc đúng những cột này để tính doanh thu và số hội viên đang hoạt động. Sai ở dashboard nguy hiểm hơn sai ở grid, vì người dùng tin vào con số đó để ra quyết định kinh doanh.

### 3.2 Giải pháp

Tạo `Entities/Enums/`, mỗi enum kiểu `: byte` nên ánh xạ vào đúng cột `tinyint` cũ — **không cần migration đổi schema**:

```csharp
public enum InvoiceStatus : byte {
    [Description("Chờ thanh toán")] Pending   = 0,
    [Description("Đã thanh toán")]  Paid      = 1,
    [Description("Đã hủy")]         Cancelled = 2,
}
```

Danh sách enum:

| Enum | Giá trị |
|---|---|
| `MemberStatus` | Active=0, Suspended=1, Expired=2 |
| `InvoiceStatus` | Pending=0, Paid=1, Cancelled=2 |
| `SubscriptionStatus` | Active=0, Expired=1, Cancelled=2 |
| `PaymentMethod` | Cash=0, BankTransfer=1, Card=2 |
| `TrainerStatus` | Working=0, Inactive=1 |
| `Gender` | Male=0, Female=1, Other=2 |
| `CheckInMethod` | Unknown=0, Card=1, Fingerprint=2 |

`CheckInMethod` hiện là `static class` chứa hằng số `const byte`; chuyển thành enum cho đồng bộ. Các chỗ dùng phải sửa theo: `FingerprintService.VerifyAsync`, entity `CheckIn`, và test liên quan.

Sửa default về `0` cho `Member.Status`, `Invoice.Status`, `MemberDataService.Status`.

### 3.3 Một nguồn nhãn duy nhất

`[Description]` là nguồn nhãn tiếng Việt duy nhất, phục vụ ba nơi:

1. `GET /api/Enums` — trả toàn bộ enum dạng `{ name, values: [{ value, label }] }`. Angular gọi một lần lúc khởi động, bỏ khai báo lặp trong 6 component.
2. Cột nhãn trong file Excel xuất ra.
3. Dropdown ràng buộc (data validation) trong file template import.

Helper `EnumLabel.ToLabel(this Enum)` đọc `[Description]` qua reflection, **có cache theo type** để không phản chiếu lại mỗi dòng Excel.

### 3.4 Dữ liệu đã có trong DB

Đổi default chỉ ảnh hưởng bản ghi mới (đây là CLR initializer, không phải `HasDefaultValue` phía DB — đã kiểm tra `OnModelCreating`). Bản ghi cũ vẫn giữ giá trị cũ.

**Quyết định: không tự động rewrite dữ liệu trong migration.** Thay vào đó kèm `docs/sql/2026-09-22-fix-status-defaults.sql` có chú thích rõ từng câu lệnh, để chủ hệ thống tự quyết định chạy hay không. Lý do: không thể biết chắc một hóa đơn `Status=1` trong DB hiện tại là "đã thanh toán thật" hay "mặc định sai" — đoán sai sẽ làm hỏng sổ sách.

## 4. Hạ tầng Excel

### 4.1 Cấu trúc

```
Infrastructure/Excel/
  ExcelColumn.cs            ExcelColumn<T>: Header, Get(T), Format, Width, IsRequired, Parse
  SheetDefinition.cs        SheetDefinition<T>: SheetName + Columns + Plus(thêm cột chỉ đọc)
  ExcelWriter.cs            Write<T>(SheetDefinition<T>, IEnumerable<T>) -> byte[]
  ExcelTemplateWriter.cs    WriteTemplate<T>(SheetDefinition<T>) -> byte[]
  ExcelReader.cs            Read<T>(Stream, SheetDefinition<T>) -> ReadResult<T>
  RowError.cs               RowNumber (số dòng Excel người dùng thấy), ColumnHeader, Message
  ReadResult.cs             Rows, Errors
```

Thư viện: **ClosedXML** (kéo theo `DocumentFormat.OpenXml`). Thêm vào `gym-management-server.csproj`.

### 4.2 Nguyên tắc cốt lõi

Khai báo cột đặt cạnh module của nó (ví dụ `Services/Members/MemberSheet.cs`), không gom vào một file chung:

```csharp
public static class MemberSheet {
    // Cột vừa đọc vừa ghi được — dùng cho export, template và parser
    public static SheetDefinition<MemberOutput> Import => new("Thành viên", [
        new("Họ và tên",     m => m.FullName,    required: true, parse: Set.FullName),
        new("Số điện thoại", m => m.PhoneNumber, required: true, parse: Set.Phone),
        new("Giới tính",     m => m.Gender.ToLabel(),            parse: Set.Gender),
        new("Trạng thái",    m => m.Status.ToLabel(),            parse: Set.Status),
    ]);

    // Export = cột import + cột chỉ đọc (khóa, dấu vết thời gian)
    public static SheetDefinition<MemberOutput> Export => Import.Plus([
        new("Ngày đăng ký", m => m.RegistrationDate, format: "dd/MM/yyyy"),
        new("Ngày tạo",     m => m.CreatedAt,        format: "dd/MM/yyyy HH:mm"),
    ]);
}
```

**Tập cột import là tập con của tập cột export, khai báo một lần.** Header của file export, file template và parser đọc lên đều bắt nguồn từ cùng một khai báo nên không thể lệch nhau. Cột chỉ đọc (ngày tạo, ID) được nối thêm cho export qua `Plus()` — chúng không có hàm `parse` nên `ExcelReader` bỏ qua nếu gặp trong file người dùng tải lên. Hệ quả có chủ đích: **file vừa export ra có thể sửa rồi import lại được ngay**, không cần người dùng xóa cột thừa.

Đây là lý do chọn khai báo tường minh thay vì engine reflection tự động.

Về giá trị `null`: `ToLabel()` là extension trên `Enum?` trả về chuỗi rỗng khi `null` (ví dụ `Member.Gender` là `byte?`). `ExcelWriter` ghi ô trống cho `null`, không ghi chữ "null".

Đánh đổi đã chấp nhận: mỗi module tốn khoảng 20 dòng khai báo. Bù lại nó type-safe — đổi tên property thì compile lỗi ngay, không âm thầm xuất ra file thiếu cột. Codebase đã có sẵn một mapper bằng reflection (`GymManagementServiceMapObjects`) âm thầm bỏ qua property lệch kiểu; thêm một lớp reflection nữa sẽ nhân đôi kiểu lỗi khó tìm đó.

### 4.3 Giới hạn

Chặn cứng **50.000 dòng** mỗi lần xuất. Vượt quá thì trả `400` kèm thông báo yêu cầu thu hẹp bộ lọc. ClosedXML dựng workbook trong bộ nhớ nên không có giới hạn này thì một request có thể làm cạn RAM.

## 5. Export — 9 module

### 5.1 API

Mỗi controller thêm:

```
GET /api/{Module}/export
```

Trả `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`, tên file dạng `thanh-vien-2026-09-22.xlsx`.

Danh sách chính xác 9 module có export:

| # | Controller | Lọc `?from=&to=` | Quyền |
|---|---|---|---|
| 1 | `Member` | không | Authorize |
| 2 | `ServicePackage` | không | Authorize |
| 3 | `Trainer` | không | Authorize |
| 4 | `AttendanceDevice` | không | Admin |
| 5 | `MemberDataService` | có | Authorize |
| 6 | `CheckIn` | có | Authorize |
| 7 | `Attendance` (lịch sử điểm danh) | có | Authorize |
| 8 | `Invoice` (kèm dòng chi tiết) | có | Admin |
| 9 | `Expense` | có | Admin |

`InvoiceItem` không có endpoint export riêng — dòng chi tiết được gộp vào file export của `Invoice` dưới dạng sheet thứ hai. Xuất riêng danh sách dòng chi tiết tách khỏi hóa đơn cha là vô nghĩa với người dùng.

**`FingerprintTemplate` cố ý KHÔNG có export.** Dữ liệu sinh trắc học không bao giờ được rời khỏi hệ thống dưới dạng file, kể cả khi đã mã hóa. Đây là quyết định có chủ đích, không phải thiếu sót — ai đọc spec này về sau đừng "bổ sung cho đủ".

### 5.2 Lấy dữ liệu

Export lấy **toàn bộ** theo bộ lọc, không theo trang. Điều này đi vòng qua luôn lỗi phân trang đang tồn tại (frontend gửi `?pageNumber=`, backend đọc `?page=` — mọi grid luôn trả trang 1); export không phụ thuộc vào việc sửa lỗi đó.

Module không có trục thời gian dùng lại `GetAllAsync()` sẵn có. Bốn module có trục thời gian thêm một method vào repository tương ứng, đặt đúng theo pattern của `GetPagedAsync`:

```csharp
Task<List<Invoice>> GetForExportAsync(DateTime? from, DateTime? to);
```

### 5.3 Frontend

- `shared/components/export-button/` — gắn vào toolbar DevExtreme DataGrid của từng grid, nhận endpoint và tên file qua `@Input`.
- `shared/services/file-download.service.ts` — xử lý blob response, đặt tên file, kích hoạt tải xuống.

## 6. Import — Thành viên và Gói dịch vụ

### 6.1 API

```
GET  /api/Member/import/template      → file .xlsx mẫu
POST /api/Member/import?dryRun=true   → kiểm tra, KHÔNG ghi gì, trả lỗi từng dòng
POST /api/Member/import?dryRun=false  → ghi trong một transaction
```

Tương tự cho `/api/ServicePackage/import`.

### 6.2 Vì sao không lưu trạng thái giữa hai bước

Bước xác nhận gửi lại chính file đó — trình duyệt vẫn giữ object `File`, người dùng không phải chọn lại — và server kiểm tra lại từ đầu.

Đổi lấy việc parse hai lần, ta loại bỏ hoàn toàn: token, cache phía server, TTL hết hạn giữa chừng, và rủi ro phình bộ nhớ khi nhiều người import cùng lúc. Với file vài nghìn dòng, chi phí parse lại không đáng kể. Thiết kế này cũng không ràng buộc hệ thống phải chạy trên một instance duy nhất.

### 6.3 Nội dung kiểm tra

- Thiếu cột bắt buộc
- Sai định dạng (ngày, số, email)
- Giá trị enum không nằm trong danh sách hợp lệ
- Trùng khóa nghiệp vụ **với DB** — số điện thoại (Thành viên), tên gói (Gói dịch vụ)
- Trùng khóa nghiệp vụ **trong nội bộ file** — trường hợp dễ bỏ sót nhất

Lỗi báo theo đúng số dòng Excel mà người dùng nhìn thấy (tính cả dòng header), kèm tên cột.

### 6.4 Ghi dữ liệu

Hành vi **tất cả-hoặc-không**: còn bất kỳ lỗi nào thì không ghi dòng nào.

Repository của hai module này thêm `AddRangeAsync(IEnumerable<T>)` — một `SaveChangesAsync` duy nhất thay vì gọi mỗi dòng một lần — bọc trong `BeginTransactionAsync()`. Lỗi giữa chừng thì rollback sạch.

### 6.5 Frontend

`shared/components/import-dialog/` — một dialog dùng chung cho cả hai module, nhận endpoint base qua tham số:

1. Chọn file (kèm nút "Tải file mẫu")
2. Gửi `dryRun=true`, hiện bảng xem trước và danh sách lỗi
3. Nếu sạch lỗi, cho phép bấm xác nhận → gửi `dryRun=false`
4. Hiện kết quả, refresh grid

## 7. Dashboard

### 7.1 Cấu trúc backend

```
Repositories/Reporting/IDashboardRepository.cs
Repositories/Reporting/DashboardRepository.cs   ← chỉ chứa truy vấn tổng hợp
Services/Reporting/DashboardService.cs
Controllers/DashboardController.cs
DTOs/Dashboard/
```

Repository riêng cho báo cáo. Không nhét `GroupBy` vào các repository CRUD sẵn có — chúng đang gọn và có một trách nhiệm rõ ràng.

### 7.2 Endpoint

Tách theo widget thay vì gộp một cục: hàng KPI phải hiện ngay khi mở trang, còn biểu đồ giờ cao điểm phải quét bảng check-in nên chậm hơn. Gộp chung thì cả trang chờ theo widget chậm nhất. Tách ra cũng cho phép thêm widget sau này mà không đụng contract cũ.

| Endpoint | Nội dung |
|---|---|
| `GET /kpi?month=YYYY-MM` | Doanh thu, chi phí, lợi nhuận, hội viên hoạt động, hội viên mới, đang trong phòng, sắp hết hạn, công nợ — kèm % so tháng trước |
| `GET /revenue-trend?months=12` | Doanh thu vs chi phí theo tháng |
| `GET /member-growth?months=12` | Hội viên mới + tổng đang hoạt động theo tháng |
| `GET /package-distribution` | Số đăng ký đang hiệu lực và doanh thu theo từng gói |
| `GET /peak-hours?days=30` | Lượt check-in theo 24 khung giờ |
| `GET /expiring-soon?days=30` | Gói sắp hết hạn, kèm số điện thoại để gọi ngay |

### 7.3 Định nghĩa số liệu

Ghi rõ vì đây là con số người dùng tin để ra quyết định:

- **Doanh thu** = tổng `Invoice.TotalAmount` với `Status = Paid`, nhóm theo `InvoiceDate`.
- **Chi phí** = tổng `Expense.Amount`, nhóm theo `ExpenseDate`.
- **Lợi nhuận** = Doanh thu − Chi phí.
- **Công nợ** = tổng `Invoice.TotalAmount` với `Status = Pending`.
- **Hội viên hoạt động** = `Member` với `Status = Active` và `IsDeleted = false`.
- **Hội viên mới** = `Member` có `RegistrationDate` trong tháng.
- **Sắp hết hạn** = `MemberDataService` với `Status = Active` và `EndDate` trong N ngày tới.

Lưu ý đã biết: `Invoice.TotalAmount` hiện do client gửi lên chứ không cộng từ `InvoiceItem`. Dashboard đọc đúng giá trị đang lưu. Việc tính tổng từ items ở server nằm ngoài phạm vi spec này.

### 7.4 Múi giờ

Việt Nam là UTC+7 cố định, không có DST từ năm 1975. Vì vậy nhóm theo tháng/giờ bằng `DATEADD(hour, 7, ...)` dịch thẳng xuống SQL là đúng và rẻ, không cần thư viện timezone.

Giả định này được ghi rõ tại chỗ trong code để sau này ai đọc cũng hiểu vì sao không dùng `TimeZoneInfo`. Nếu hệ thống mở rộng ra nhiều múi giờ thì đây là điểm cần sửa đầu tiên.

Toàn bộ tổng hợp chạy `GroupBy` dịch xuống SQL, không kéo bảng lên bộ nhớ.

### 7.5 Frontend

- Module lazy `modules/dashboard/`, dùng `chart.js` + `ng2-charts` đã có sẵn.
- Mỗi widget là một component con, tự gọi endpoint của mình và tự hiện trạng thái loading — không chờ nhau.
- Route `/dashboard` thay `/members` làm redirect mặc định; thêm vào đầu sidebar.
- Giữ nguyên `/attendance-dashboard` — nó phục vụ mục đích khác (theo dõi thời gian thực ai đang trong phòng), không gộp.

## 8. Phân quyền

Mọi endpoint mới đều `[Authorize]`.

Nhóm tài chính giới hạn `Roles = "1"` (Admin): `/revenue-trend`, `/package-distribution`, export `Invoice` và `Expense`. Nhân viên lễ tân không cần thấy lợi nhuận của chủ.

Export `AttendanceDevice` cũng là Admin, bám theo `[Authorize(Roles = "1")]` sẵn có trên `AttendanceDeviceController` — endpoint export không được nới lỏng hơn endpoint đọc dữ liệu của cùng module đó. Quy tắc chung: **quyền của `/export` luôn bằng quyền của `GET` cùng controller.**

`/kpi` là ngoại lệ: trả về DTO có các trường tài chính (doanh thu, chi phí, lợi nhuận, công nợ) đặt `null` khi người gọi không phải Admin, thay vì trả `403` cho cả endpoint — để nhân viên vẫn xem được phần vận hành trên cùng một trang.

**Ngoài phạm vi, cần biết:** phần lớn API nghiệp vụ hiện có (`Member`, `Invoice`, `Expense`, `Trainer`, `ServicePackage`, `CheckIn`, `MemberDataService`) đang **không** yêu cầu đăng nhập. Endpoint mới trong spec này được bảo vệ đúng cách, nhưng dữ liệu chúng phục vụ vẫn truy cập được qua các endpoint cũ. Việc vá là Giai đoạn 0 trong lộ trình riêng, không thuộc spec này.

## 9. Kiểm thử

Repo hiện có 28 test, **toàn bộ thuộc module vân tay**. Phần này là nhóm test thứ hai có ý nghĩa.

**Unit**
- `ExcelWriter` sinh đúng header và giá trị từ `SheetDefinition`
- `ExcelReader` báo đúng số dòng Excel khi gặp lỗi (kiểm cả lệch off-by-one do dòng header)
- `ExcelReader` phát hiện trùng khóa trong nội bộ file
- `EnumLabel.ToLabel` đọc đúng `[Description]`; cache hoạt động
- Mốc đầu/cuối tháng và khung giờ theo giờ Việt Nam

**Integration**
- Export trả đúng content-type và đúng số dòng
- Export vượt 50.000 dòng trả `400`
- Import `dryRun=true` với file sai → trả lỗi và **không ghi một dòng nào**
- Import thất bại giữa chừng → rollback về đúng 0 bản ghi
- `/kpi` trả đúng số với dữ liệu seed đã biết trước
- Endpoint tài chính trả `403` với user role Staff; `/kpi` trả trường tài chính `null`

**Frontend**
- Dashboard render KPI từ service giả lập
- `ImportDialogComponent` hiện danh sách lỗi và khóa nút xác nhận khi còn lỗi

## 10. Thứ tự triển khai

1. **Enum** — phải xong trước, vì cả Excel lẫn Dashboard đều đọc nhãn từ đó
2. **Hạ tầng Excel + Export 9 module**
3. **Import Thành viên và Gói dịch vụ**
4. **Dashboard**

Mỗi bước độc lập kiểm thử và có thể merge riêng.

## 11. Quyết định đã chốt

| Quyết định | Lựa chọn | Lý do |
|---|---|---|
| Engine Excel | Khai báo cột tường minh | Type-safe; một khai báo dùng cho cả export, template và parser |
| Nơi xuất file | Server-side (ClosedXML) | Xuất được toàn bộ dữ liệu, không vướng lỗi phân trang |
| Phạm vi import | Chỉ Thành viên + Gói dịch vụ | Hai tập cần nhập hàng loạt thật; import dữ liệu tài chính rủi ro cao, giá trị thấp |
| Hành vi import | Xem trước, tất cả-hoặc-không | Dữ liệu không bao giờ ở trạng thái nửa vời |
| Trạng thái giữa 2 bước import | Không lưu, gửi lại file | Bỏ được token, cache, TTL và rủi ro phình bộ nhớ |
| Kiến trúc dashboard | Tách endpoint theo widget | KPI hiện ngay, widget chậm không chặn cả trang |
| Múi giờ | Offset cố định +7 trong SQL | VN không có DST; rẻ và đúng |
| Dữ liệu status cũ | Script SQL thủ công, không tự chạy | Không thể đoán ý nghĩa dữ liệu tài chính đã có |
