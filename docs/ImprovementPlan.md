# Kế hoạch cải thiện hệ thống (2026-10)

Tài liệu này ghi lại **hiện trạng thực tế** (đã khảo sát code) và **đề xuất kỹ thuật** cho 5 hạng
mục cải thiện được yêu cầu, cộng thêm **mục 6 (audit log)** phát hiện trong lúc review là một lỗ
hổng đáng kể nên bổ sung. Mục tiêu là làm rõ "đang có gì", "đang thiếu gì", và "làm thế nào" trước
khi bắt tay code, để tránh thiết kế lại giữa chừng.

---

## 1. Tài khoản đăng nhập, đa phòng gym (multi-tenant), phân quyền, chịu tải đăng nhập đồng thời

### Hiện trạng
- `Entities/Users/User.cs`: chỉ có `Role` dạng `byte` (0 = Staff, 1 = Admin). Không có bảng
  Role/Permission riêng — phân quyền hiện tại là so khớp chuỗi số (`[Authorize(Roles = "1")]`,
  `[Authorize(Roles = "0,1")]`) rải rác trên từng controller.
- **Không có khái niệm Tenant/Branch** ở bất kỳ đâu trong schema. Không có cột `TenantId` trên
  `Member`, `Trainer`, `Invoice`,... → hệ thống đang **single-tenant** theo thiết kế.
- `POST /api/Auth/register` (`AuthService.cs:34-57`) là **endpoint công khai, không cần đăng
  nhập**, và luôn tạo tài khoản với `Role = 0`. Không có API hay màn hình Admin nào để **tạo tài
  khoản cho người khác** — cách duy nhất để có Admin là qua migration seed hoặc sửa tay DB.
- JWT (`AuthService.GenerateAuthOutput`) chỉ chứa `NameIdentifier`, `Name`, `Email`, `Role`.

### Vấn đề / rủi ro
- Không thể vận hành nhiều phòng gym độc lập — dữ liệu của tất cả chi nhánh nằm chung, không
  có cơ chế cô lập (data isolation).
- Phân quyền dạng "numeric role string" khó mở rộng khi cần vai trò mới (vd: Lễ tân chỉ được
  check-in, không được xem báo cáo doanh thu).
- Tự đăng ký công khai → rủi ro bảo mật (ai cũng tạo được tài khoản Staff).

### Đề xuất
1. **Thêm entity `Tenant`** (`Id, Name, Address, Phone, IsActive, CreatedAt`) làm đơn vị scope.
2. **Thêm `TenantId` (FK, not null)** vào các entity cần cô lập theo chi nhánh: `Member`, `Trainer`,
   `ServicePackage`, `Invoice`, `InvoiceItem`, `Expense`, `CheckIn`, `AttendanceDevice`, `User`.
3. Dùng **EF Core Global Query Filter** (`HasQueryFilter(e => e.TenantId == _currentTenantId)`) để mọi
   query tự động lọc theo tenant hiện tại, tránh phải sửa từng Service/Repository.
4. Thêm claim `TenantId` vào JWT khi login; một `ICurrentTenantAccessor` (scoped service) đọc claim này
   để cấp cho query filter ở bước 3.
5. Thay model Role đơn giản bằng **Role mở rộng dạng enum rõ ràng** (`Admin, Manager, Trainer,
   Receptionist`) — đủ dùng ở quy mô hiện tại, không cần bảng Permission riêng nếu chưa cần phân
   quyền theo từng hành động cụ thể. Nếu sau này cần phân quyền chi tiết hơn (vd: "chỉ được sửa,
   không được xoá"), nâng cấp thành bảng `Role` + `Permission` + `RolePermission`.
6. **Khoá `register` lại**: chỉ Admin của một tenant (một phòng gym cụ thể) mới được tạo tài khoản
   mới trong tenant đó (`POST /api/Users`, `[Authorize(Roles = "Admin")]`, nhận `TenantId` từ claim
   của người gọi, không cho client tự chọn tenant). Giữ `register` công khai chỉ cho trường hợp
   **tạo tenant mới (phòng gym mới) + Admin đầu tiên** (self-service onboarding), tách riêng khỏi
   luồng "thêm nhân viên".
7. Thêm màn hình Angular "Quản lý tài khoản" (module mới `users-module`) cho Admin: danh sách,
   tạo, khoá/mở tài khoản, đổi vai trò.
8. **Chiến lược migrate dữ liệu hiện có** (quan trọng, dễ bị bỏ sót): thêm `TenantId` dạng
   **nullable** trước, tạo sẵn 1 tenant mặc định (vd: "Chi nhánh mặc định"), chạy script backfill
   gán toàn bộ dữ liệu hiện có (Member, Invoice, User,...) vào tenant này, **sau đó** mới đổi cột
   thành NOT NULL và bật Global Query Filter. Làm đúng thứ tự này để tránh gãy migration trên dữ
   liệu production đang có — không set NOT NULL ngay từ đầu.
9. Mọi hành động tạo/khoá/mở tài khoản, đổi vai trò nên được ghi vào audit log — xem
   **[mục 6](#6-audit-log--ghi-lại-ai-đã-làm-gì-mục-bổ-sung)**.

### Về "hỗ trợ nhiều người dùng đăng nhập không làm chậm hệ thống"
JWT hiện tại đã **stateless** (không lưu session server-side) — đây là điểm tốt, cho phép scale
ngang (chạy nhiều instance API phía sau load balancer mà không cần sticky session). Để thực sự
chịu tải đăng nhập đồng thời tốt:
- Xác nhận `AuthService.LoginAsync` và các thao tác DB là **async** xuyên suốt (không block thread).
- Dùng thuật toán băm mật khẩu có work-factor hợp lý (BCrypt cost 10–12) — cao quá sẽ làm login
  chậm khi tải cao, thấp quá thì yếu bảo mật.
- Đánh index trên `Username`/`Email` (cột dùng để tra cứu khi login).
- Thêm rate-limiting trên `/login` để chống brute-force mà không cần chặn người dùng hợp lệ.
- Khi có nhiều gym, cân nhắc nâng cấp từ SQL Server Express (`appsettings.json` đang trỏ
  `DESKTOP-xxx\SQLEXPRESS`) lên instance SQL Server đầy đủ / Azure SQL để tránh giới hạn kết nối
  đồng thời của bản Express.

### Độ ưu tiên: **Cao** (ảnh hưởng schema rộng — nên làm sớm để các tính năng sau không phải
retrofit lại `TenantId`), **Độ phức tạp: Lớn** (migration cho nhiều bảng, cần kế hoạch di chuyển dữ
liệu hiện có vào tenant "mặc định").

---

## 2. Luôn kiểm tra concurrency (xung đột dữ liệu đồng thời)

### Hiện trạng
- Optimistic concurrency (`RowVersion` + `[Timestamp]`) **chỉ tồn tại ở 2 entity**:
  `FingerprintTemplate` và `AttendanceDevice` (thêm khi làm tính năng vân tay).
- **Không có** `RowVersion` trên `Member`, `CheckIn`, `Invoice`, `InvoiceItem`, `Trainer`,
  `ServicePackage`, `Expense`, `User`.
- **Không có bất kỳ xử lý `DbUpdateConcurrencyException` nào** trong toàn bộ codebase → nếu xảy
  ra xung đột, request sẽ lỗi 500 thô thay vì trả lỗi rõ ràng cho client.
- Đáng chú ý nhất: `FingerprintService.VerifyAsync` (check-in/check-out qua quét vân tay) đọc
  "có phiên đang mở hay không" rồi mới ghi — đây là **race condition kinh điển**: hai lượt quét
  gần như đồng thời cho cùng một hội viên có thể cùng thấy "chưa có phiên mở" → tạo **hai bản ghi
  check-in trùng nhau**.

### Đề xuất
1. Thêm `RowVersion` (`byte[]`, `[Timestamp]`) cho tất cả entity có thể bị sửa đồng thời bởi
   nhiều người dùng: `Member`, `CheckIn`, `Invoice`, `InvoiceItem`, `Trainer`, `ServicePackage`,
   `Expense`, `User`.
2. Thêm xử lý tập trung cho `DbUpdateConcurrencyException`: một **exception filter / middleware
   dùng chung** bắt exception này ở mọi controller, trả về `409 Conflict` kèm thông điệp rõ ràng
   ("Dữ liệu đã bị thay đổi bởi người khác, vui lòng tải lại") thay vì để lộ lỗi 500.
3. Sửa riêng race condition ở check-in/check-out (ưu tiên cao, vì ảnh hưởng trực tiếp tính đúng
   của dữ liệu điểm danh):
   - Thêm **unique filtered index** ở DB: `CREATE UNIQUE INDEX ... ON CheckIns(MemberId) WHERE
     CheckOutTime IS NULL` — đảm bảo **ở tầng database** một hội viên không thể có 2 phiên đang mở
     cùng lúc, bất kể race condition ở tầng ứng dụng.
   - Khi insert bị vi phạm unique index (do quét trùng lúc), bắt lỗi và coi đó như một check-out
     hợp lệ (retry logic) thay vì báo lỗi cho người dùng.
4. Việc này nên làm **trước hoặc cùng lúc** với mục 4 (auto check-out), vì auto-checkout job và
   thao tác check-out thủ công/quét vân tay có thể đụng vào cùng một bản ghi cùng lúc.
5. **Viết integration test giả lập race condition**: gửi 2 request `VerifyAsync` gần như đồng thời
   (`Task.WhenAll`) cho cùng một hội viên, xác nhận chỉ có **đúng 1** bản ghi check-in được tạo và
   lượt thứ 2 được xử lý thành check-out — loại lỗi này gần như không thể bắt được bằng test thủ
   công tuần tự.

### Độ ưu tiên: **Cao** (sửa lỗi dữ liệu tiềm ẩn đã tồn tại), **Độ phức tạp: Trung bình**.

---

## 3. Hình ảnh hội viên: hiển thị khi thêm/chụp, import/export

### Hiện trạng — **phần lớn đã được xây dựng, không phải làm từ đầu**
- `Member.AvatarUrl` đã có trong entity + DTO (`MemberOutput`).
- Backend đã có `POST /api/Member/{id}/avatar` (upload) và `GET /api/Member/{id}/avatar`
  (`MemberController.cs`, `MemberService.UploadAvatarAsync`/`GetAvatarPathAsync`) — lưu file vào
  `wwwroot/uploads/avatars/{id}{ext}`, tự xoá ảnh cũ khi upload ảnh mới.
- Frontend (`members/components/member-list`) **đã có sẵn**: dialog chụp ảnh qua webcam
  (`CameraDialogComponent`) và chọn file từ máy (`onFileSelected`), cả hai đều gọi
  `MemberService.uploadAvatar(...)`.

### Vấn đề / khoảng trống thực sự còn lại
- Lưu trữ file **trực tiếp trên đĩa cục bộ** (`wwwroot`) — không phù hợp khi scale ra nhiều
  instance server (liên quan mục 1) vì mỗi instance có đĩa riêng, ảnh upload vào instance A sẽ
  không thấy được từ instance B.
- Không có validate loại file thực sự (magic byte, chỉ dựa vào đuôi file) hay giới hạn kích thước
  — rủi ro upload file giả dạng ảnh.
- Không có resize/thumbnail — ảnh gốc được phục vụ trực tiếp trong danh sách, tốn băng thông khi
  danh sách hội viên dài.
- **Chưa có tính năng import/export hàng loạt** ảnh hội viên (theo yêu cầu).

### Đề xuất
1. Validate file khi upload: kiểm tra magic byte (không chỉ đuôi file), giới hạn kích thước
   (vd: 5MB), giới hạn định dạng (jpg/png/webp).
2. Sinh thêm bản **thumbnail** (vd: 150x150) song song với ảnh gốc khi upload, dùng thumbnail cho
   lưới danh sách để giảm tải, ảnh gốc chỉ tải khi xem chi tiết.
3. **Import hàng loạt**: endpoint nhận file `.zip`, quy ước đặt tên file theo `MemberId` hoặc mã
   hội viên (vd: `{MemberCode}.jpg`), xử lý từng ảnh, trả về báo cáo (file nào thành công/thất bại
   và lý do).
4. **Export hàng loạt**: endpoint xuất ảnh của toàn bộ hoặc một danh sách hội viên đã lọc thành
   file `.zip` (hữu ích khi cần in thẻ hội viên hàng loạt).
5. Khi hệ thống đã multi-tenant (mục 1) hoặc cần scale, cân nhắc chuyển lưu trữ ảnh sang
   **object storage** (S3-compatible / Azure Blob — README đã nêu định hướng AWS) thay vì đĩa cục
   bộ, để mọi instance API đều truy cập được cùng một nơi lưu ảnh.

### Độ ưu tiên: **Trung bình** (phần hiển thị/chụp ảnh cơ bản đã chạy được), **Độ phức tạp: Nhỏ–
Trung bình** cho import/export, riêng việc chuyển sang object storage là việc lớn hơn nên gộp
chung với kế hoạch scale ở mục 1.

---

## 4. Check-out: xử lý khi không check-out, tự động check-out sau 24h

### Hiện trạng
- `CheckIn` entity: `CheckInTime`, `CheckOutTime` (nullable — `null` = đang trong phòng gym),
  `Method` (enum: Unknown/Card/Fingerprint). Không có cột phân biệt **cách thức check-out**.
- Check-out hiện tại chỉ xảy ra qua **cơ chế toggle của quét vân tay**
  (`FingerprintService.VerifyAsync`): quét lần 1 → check-in, quét lần 2 → check-out bản ghi đang
  mở. Không có nút/API "check-out thủ công" riêng cho nhân viên.
- **Không có background job / scheduled service nào trong hệ thống** (không Hangfire, không
  Quartz, không `IHostedService`). → Một hội viên check-in rồi **không quét ra** (quên, máy lỗi,
  ra cửa khác...) sẽ ở trạng thái "đang trong gym" **mãi mãi**, làm sai lệch
  `GetActiveAttendanceAsync` và các số liệu dashboard liên quan.

### Đề xuất
1. Thêm cột **`CheckOutMethod`** (enum: `None`, `Scan`, `ManualByStaff`, `AutoTimeout`) vào
   `CheckIn` — ghi rõ check-out xảy ra bằng cách nào, phục vụ báo cáo/audit.
2. Thêm **`AutoCheckoutBackgroundService`** (`IHostedService`/`BackgroundService`) chạy định kỳ
   (vd: mỗi 15–30 phút): tìm các `CheckIn` có `CheckOutTime IS NULL` và
   `CheckInTime < now - 24h`, đặt `CheckOutTime` (vd: `CheckInTime + 24h`) và
   `CheckOutMethod = AutoTimeout`.
   - Áp dụng cùng cơ chế concurrency ở mục 2 (unique index / RowVersion) để job này không đụng độ
     với một lượt quét/check-out thủ công xảy ra đúng lúc job chạy.
3. Thêm **API check-out thủ công cho nhân viên**: `POST /api/checkins/{id}/checkout` trên
   `CheckInController` — validate `CheckOutTime` phải sau `CheckInTime`, đặt
   `CheckOutMethod = ManualByStaff`, ghi `OperatorUserId` (cột đã có sẵn trong entity).
4. Thêm UI: trong màn hình đang điểm danh (active attendance), thêm nút "Check-out thủ công" cho
   nhân viên dùng khi hội viên quên quét ra.
5. Cân nhắc ngưỡng 24h có nên **cấu hình được** theo từng tenant (thay vì hard-code toàn hệ thống),
   vì mỗi phòng gym có thể muốn ngưỡng khác nhau (ví dụ gym 24/7 vs gym đóng cửa ban đêm).
6. **Viết test cho `AutoCheckoutBackgroundService`** mà không cần chờ thực 24h: tiêm thời gian qua
   một abstraction (`TimeProvider`/`ISystemClock`) thay vì gọi `DateTime.UtcNow` trực tiếp trong
   service, rồi "tua" thời gian trong test để xác nhận các `CheckIn` quá hạn được đóng đúng với
   `CheckOutMethod = AutoTimeout`, còn các phiên chưa quá hạn thì không bị đụng vào.

### Độ ưu tiên: **Cao** (dữ liệu điểm danh sai lệch ảnh hưởng trực tiếp tới báo cáo/doanh thu liên
quan đến gói tập), **Độ phức tạp: Trung bình**.

---

## 5. Hỗ trợ quét khuôn mặt (face recognition)

### Hiện trạng — nền tảng tái sử dụng được
Kiến trúc vân tay đã tách lớp rất rõ ràng và được tài liệu hoá là **vendor-neutral**
(`docs/FingerprintAttendance.md`):
- `IFingerprintProvider { Vendor; CreateTemplate(byte[]); Identify(probe, candidates); }`
- `IFingerprintProviderFactory` — chọn provider theo `Vendor`, đăng ký qua DI (`Program.cs`).
- `ITemplateProtector`/`AesTemplateProtector` — mã hoá template tại rest bằng AES-256-GCM, chỉ
  giải mã tạm thời trong RAM khi so khớp.
- Matching chạy **server-side**, business logic không phụ thuộc SDK hãng nào.

Đây chính xác là loại kiến trúc cần để thêm một thiết bị sinh trắc học mới mà **không phải sửa
`FingerprintService`/controller hiện có**.

### Đề xuất — 2 hướng, khuyến nghị hướng B

**Hướng A — Làm song song (ít rủi ro, nhanh hơn):**
Tạo bộ interface/entity riêng cho khuôn mặt, giống hệt khuôn mẫu vân tay:
`IFaceRecognitionProvider`, `FaceRecognitionProviderFactory`, entity `FaceTemplate`
(`MemberId, Vendor, EncryptedTemplate, RowVersion`), thêm `CheckInMethod.Face`. Nhược điểm: hai
pipeline gần như giống hệt nhau tồn tại song song → trùng lặp code, khó bảo trì khi thêm modality
thứ 3 (vd: QR code) sau này.

**Hướng B — Hợp nhất thành abstraction sinh trắc học chung (khuyến nghị nếu dự tính hỗ trợ ≥2
loại thiết bị):**
- Thêm `BiometricModality` enum (`Fingerprint, Face`).
- Gộp `FingerprintTemplate` thành `BiometricTemplate` chung (`MemberId, Modality, Vendor,
  EncryptedTemplate, RowVersion`) — cần migration chuyển dữ liệu vân tay hiện có sang bảng mới.
- Một interface chung `IBiometricProvider { Modality; byte[] CreateTemplate(byte[] raw);
  BiometricMatchResult Identify(byte[] probe, candidates); }`, factory chọn theo cả `Modality` lẫn
  `Vendor`.
- Tái sử dụng nguyên `ITemplateProtector` (đã modality-agnostic).

### Các vấn đề riêng của khuôn mặt (khác với vân tay, cần lưu ý khi thiết kế)
- **Liveness detection / chống giả mạo** (ảnh/ video giả): nên dựa vào khả năng của thiết bị/SDK
  hãng (camera depth, IR) thay vì tự xây dựng — nếu chọn thiết bị không hỗ trợ liveness, rủi ro bị
  qua mặt bằng ảnh in.
- Template khuôn mặt thường là **embedding vector** (so khớp bằng cosine similarity), không phải
  minutiae như vân tay — `Identify` cần cho phép mỗi provider tự định nghĩa cách tính điểm khớp
  (đã tương thích với interface hiện tại vì `Identify` đã trả `Score` dạng số chung chung).
- Có thể tái dùng ngay `CameraDialogComponent` (Angular, đang dùng để chụp ảnh hội viên ở mục 3)
  làm điểm bắt đầu cho giao diện "đăng ký khuôn mặt" — tận dụng hạ tầng webcam đã có.
- **Pháp lý**: dữ liệu khuôn mặt là dữ liệu sinh trắc học nhạy cảm theo Nghị định 13/2023/NĐ-CP về
  bảo vệ dữ liệu cá nhân — cần màn hình xin sự đồng ý rõ ràng (consent) khi đăng ký khuôn mặt, và
  áp dụng chính sách mã hoá/lưu trữ/xoá tương đương hoặc chặt hơn những gì đang làm với vân tay.
- Bắt đầu bằng một `MockFaceRecognitionProvider` (giống `MockFingerprintProvider` hiện tại) để
  hoàn thiện luồng nghiệp vụ trước khi tích hợp SDK thật của một hãng cụ thể.

### Độ ưu tiên: **Thấp–Trung bình** (tính năng mới, không sửa lỗi hiện có), **Độ phức tạp: Lớn**
nếu đi hướng B (cần migrate dữ liệu vân tay hiện có).

---

## 6. Audit log — ghi lại ai đã làm gì (mục bổ sung)

Phát sinh từ câu hỏi "cần log chỗ nào khác" khi review mục 1 (quản lý tài khoản). Đã rà lại toàn
bộ entity để xác định chỗ nào đang thiếu dấu vết "ai đã sửa gì, khi nào".

### Hiện trạng — rà theo từng entity (`Entities/**/*.cs`)
| Entity | Có `CreatedAt`/`UpdatedAt`? | Có "ai đã làm" (`CreatedBy`/`OperatorUserId`)? |
|---|---|---|
| `AttendanceDevice`, `FingerprintTemplate` | Có | Có `CreatedBy` (không có `UpdatedBy`) |
| `Member`, `User`, `ServicePackage`, `MemberDataService` | Có | **Không** |
| `CheckIn` | Không (chỉ có `CheckInTime`/`CheckOutTime`) | Có `OperatorUserId`, nhưng chỉ ghi khi tạo — **sửa lại sau thì không có vết** |
| `Invoice`, `InvoiceItem`, `Expense`, `Trainer` | **Không có gì cả** | **Không có gì cả** |

- Không có bảng `AuditLog` hay cơ chế ghi log thay đổi nào trong toàn bộ codebase (đã grep
  `AuditLog`, `ILogger`, `Serilog` — không có kết quả liên quan).
- **Đáng lo nhất: `Invoice`, `InvoiceItem`, `Expense`** (dữ liệu tiền) **và `Trainer`** hoàn toàn
  không có `CreatedAt`/`UpdatedAt` hay người thực hiện — một khoản thu/chi có thể bị sửa hoặc xoá
  mà không để lại dấu vết gì, kể cả thời điểm thay đổi.
- `CheckIn` có `OperatorUserId` cho hành động tạo (quét vân tay/thêm thủ công), nhưng nếu ai đó
  sửa `CheckInTime`/`CheckOutTime` qua API CRUD chung sau đó, không có gì ghi lại giá trị cũ hay
  ai đã sửa — rủi ro gian lận chấm công.
- `Member` (dữ liệu cá nhân hội viên) chỉ có `UpdatedAt`, không biết **ai** đã sửa thông tin gì —
  liên quan tuân thủ Nghị định 13/2023/NĐ-CP về bảo vệ dữ liệu cá nhân.

### Đề xuất
Thay vì thêm `CreatedBy`/`UpdatedBy` thủ công vào từng entity (chắp vá, dễ quên khi thêm entity
mới), xây **một cơ chế audit log dùng chung**:
1. Entity mới `AuditLog(Id, TenantId, EntityName, EntityId, Action [Create/Update/Delete],
   ActorUserId, ActorUsername, Timestamp, OldValuesJson, NewValuesJson)`. Lưu `ActorUsername` kèm
   theo (không chỉ Id) để vẫn đọc được lịch sử nếu tài khoản đó sau này bị xoá.
2. Override `SaveChangesAsync` trong `GymManagementContext`: trước khi lưu, duyệt
   `ChangeTracker.Entries()`, với các entity nằm trong **danh sách cần audit**, chụp lại giá trị cũ
   (`OriginalValues`) và mới (`CurrentValues`) thành JSON, ghi một dòng `AuditLog` trong cùng
   transaction với thay đổi gốc.
3. **Danh sách entity cần audit, theo độ ưu tiên** (dựa trên khoảng trống đã tìm thấy ở trên):
   `User` (gắn với mục 1), `Invoice`/`InvoiceItem`/`Expense` (đang thiếu hoàn toàn, rủi ro cao
   nhất), `CheckIn` (chống sửa chấm công sau khi tạo, gắn với mục 4), `Member` (tuân thủ dữ liệu cá
   nhân), `ServicePackage` (thay đổi giá/thời hạn ảnh hưởng doanh thu).
4. Lấy actor từ một `ICurrentUserAccessor` (scoped, đọc `HttpContext.User` claim) — nên gộp chung
   với `ICurrentTenantAccessor` ở mục 1 thành một service đọc cả `UserId` lẫn `TenantId` từ JWT.
5. **Loại trừ** dữ liệu sinh trắc học khỏi `OldValuesJson`/`NewValuesJson` khi audit
   `FingerprintTemplate` (và `FaceTemplate` sau này ở mục 5) — chỉ log metadata (Vendor, MemberId),
   tuyệt đối không log bytes của template đã mã hoá vào audit log.
6. Thêm màn hình Admin "Lịch sử thay đổi" (read-only), lọc theo entity / người thực hiện / khoảng
   thời gian — tái sử dụng cho cả nhu cầu đối soát doanh thu lẫn tra soát chấm công.

### Độ ưu tiên: **Cao** cho phần `Invoice`/`InvoiceItem`/`Expense`/`CheckIn` (đang là lỗ hổng dữ
liệu tài chính hoàn toàn không có vết), **Trung bình** cho `Member`/`ServicePackage`.
**Độ phức tạp: Trung bình** cho hạ tầng chung, sau đó mỗi entity thêm vào danh sách audit gần như
miễn phí.

---

## Đề xuất thứ tự triển khai

| Thứ tự | Hạng mục | Lý do |
|---|---|---|
| 1 | Mục 2 — Concurrency | Sửa lỗi dữ liệu đang tiềm ẩn (race condition check-in/out), ít phụ thuộc, nên làm trước khi xây thêm tính năng lên trên phần dữ liệu CheckIn. |
| 2 | Mục 6 — Audit log (hạ tầng chung + `Invoice`/`Expense`/`CheckIn`/`User`) | Lấp lỗ hổng lớn nhất hiện tại (dữ liệu tài chính không có vết thay đổi); đồng thời dựng sẵn `ICurrentUserAccessor` sẽ được tái dùng ở bước 3 (ghi ai check-out thủ công) và bước 5 (log quản lý tài khoản). |
| 3 | Mục 4 — Check-out/auto-checkout | Phụ thuộc trực tiếp vào cơ chế concurrency ở bước 1 và dùng `ICurrentUserAccessor` từ bước 2; giải quyết lỗ hổng dữ liệu điểm danh rõ ràng nhất hiện tại. |
| 4 | Mục 3 — Import/export ảnh | Việc hiển thị/chụp ảnh đã có sẵn, phần còn thiếu (import/export) độc lập, rủi ro thấp, có thể làm song song với các mục khác. |
| 5 | Mục 1 — Multi-tenant & phân quyền | Khối lượng lớn nhất, đụng vào schema của hầu hết entity — nên làm khi đã ổn định các phần trên, và làm **trước** mục 5 (face) để thiết bị mới sinh ra sau này đã gắn đúng theo tenant. Khi xong, bổ sung `TenantId` vào bảng `AuditLog` ở bước 2. |
| 6 | Mục 5 — Face recognition | Tính năng mới hoàn toàn, nên làm sau khi đa-tenant đã ổn định vì thiết bị khuôn mặt cũng cần gắn với một tenant cụ thể giống `AttendanceDevice` hiện tại. |

Mỗi hạng mục ở trên là một migration/PR riêng — không gộp chung để dễ review và rollback nếu cần.
