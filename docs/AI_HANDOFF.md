# Bàn giao công việc AI — MindSprintAI_ASP.NET_MVC

## Quyết định của người dùng

- 2026-11-14: hướng 1 — lấy QuizEduManager_MVC (dự án cá nhân của người dùng) làm nền logic + database cho project nhóm; **giao diện dùng của nhóm**. Claude là người thiết kế và duy trì codebase; quyết định nghiệp vụ vẫn hỏi người dùng.
- Database: SQL Server localhost, DB `MindSprintAI`; một migration khởi tạo mới (không mang 25 migration lịch sử của QuizEdu); lấy toàn bộ bảng QuizEdu, bảng riêng của nhóm (Kanban/Task…) thêm sau khi đọc file phân công nhóm.

## Nhật ký

### 2026-11-14 — Claude: bước 1, database

- `git init` + commit template gốc.
- Chép `Data/AppDbContext.cs`, `Data/AppDbContextFactory.cs`, `Data/Entities/*` (18 file) từ QuizEdu, đổi namespace `QuizEduManager_MVC` → `MindSprintAI_ASP.NET_MVC`, DB → `MindSprintAI`.
- **Tạm bỏ** khỏi `AppDbContext`: thuộc tính `Audit` và `SaveChangesAsync` (chốt lượt kết thúc qua `AttemptRules.FinalizeEndedAsync`, tăng `Version` của `IVersioned`, ghi `AuditEntry` sau savepoint). Lý do: chúng gọi `AuditLog`, `AttemptRules`, `ScreenMonitoringRules` thuộc tầng service. **Bước service phải thêm lại nguyên văn** từ `Project_Tham_Khao/QuizEduManager_MVC/Data/AppDbContext.cs` — thiếu thì chưa có khóa lạc quan (Version) và nhật ký.
- Gói: Identity.EntityFrameworkCore, EF Core SqlServer/Design 10.0.14, ghim System.Security.Cryptography.Pkcs 10.0.14. `dotnet-tools.json` (dotnet-ef 10.0.14). Chuỗi kết nối ở `appsettings.Development.json`. `Program.cs` đăng ký `AppDbContext` (tắt MARS qua `AuditConnectionString`).
- Migration `KhoiTao` (toàn bộ schema) + `BatRcsi` (bật READ_COMMITTED_SNAPSHOT, tách riêng vì không chạy trong giao dịch).
- **Đã kiểm:** build 0 lỗi/0 cảnh báo; `AppDbContextModelSnapshot.cs` **giống hệt** bản QuizEdu sau khi đổi namespace (3254 dòng, diff rỗng); `database update` tạo 53 bảng, 146 index, RCSI=1; chạy web, trang chủ 200.
- **Chưa:** test (project test của QuizEdu chưa mang sang); bảng riêng của nhóm; Identity mới chỉ có bảng, chưa cấu hình đăng nhập (bước backend).

### 2026-11-14 — Claude: bước 2, tầng service

- Người dùng: file phân công nhóm đã lỗi thời → Claude tự xác định thứ tự.
- Chép `Services/` (97 file), `Models/`, `Hubs/` từ QuizEdu. Chưa chép `TagHelpers/` (thuộc view).
- **Đổi thương hiệu** (script sed): `Brand.Name` và mọi chữ người dùng thấy "Rekal" → "MindSprint AI"; purpose Data Protection / muối băm `QuizEduManager…`/`Rekal.…` → `MindSprintAI…`/`MindSprint.…` (DB mới, không có dữ liệu cũ cần giải mã); file nhật ký `mindsprint-*.log`; cookie `rekal_machine_hint` → `mindsprint_machine_hint`; `SetApplicationName("MindSprintAI")`. Giữ tên cookie nội bộ `qem_*` (người dùng không thấy, test dựa vào). `rekal.id.vn` trong chú thích/test → `mindsprint.example`. Bình luận "theo màu rekal.css" trong `EmailLayout.cs` còn đúng màu QuizEdu — **đổi khi làm giao diện nhóm**.
- `AppDbContext` có lại `Audit` + `SaveChangesAsync` nguyên văn. `Program.cs`, `appsettings*.json` chép từ QuizEdu (Turnstile dev là khóa thử công khai của Cloudflare; Admin dev `gv.lan@test.local`). `dotnet user-secrets init` (UserSecretsId mới) để bí mật thật (SMTP, Google, Gemini…) không vào Git.
- Gói thêm: Serilog.AspNetCore 10.0.0, Serilog.Sinks.File 7.0.0, MaxMind.GeoIP2 6.1.0, Authentication.Google/MicrosoftAccount 10.0.14, ClosedXML 0.105.1.
- 3 cảnh báo CS8604 (`LearnConnectionService.cs:72,102`, `ScreenMonitoring.cs:218`, `Participant.UserId` có thể null) **có sẵn ở QuizEdu**. Đã đọc: mọi chỗ tạo Participant đều gán UserId (khách cũng có tài khoản ẩn) → báo nhầm; giữ nguyên vì là code khóa (sửa phải qua rà đối kháng).
- **Test:** project `tests/MindSprintAI_ASP.NET_MVC.Tests` đặt trong thư mục project giống QuizEdu (để `Tang8PureTests.Root` dò `Program.cs` vẫn đúng); DB test `MindSprintAI_Tests`, biến môi trường `MINDSPRINT_TEST_DB`. Mang 20 file test chỉ-service; `SafetyEmail` tách thành file riêng (khi mang `Tang5bTests` sang thì xóa bản trùng). Thay `Tang2RuleTests.Migration_ChiThemBangVaKhoaNgoai_KhongSuaBangCu` (riêng migration Tang2 không còn) bằng `Migration_SauKhoiTao_ChiThemKhongXoaHayDoiTen` — tự kiểm luật "migration chỉ thêm".
- **Đã kiểm:** build web + test 0 lỗi (3 cảnh báo có sẵn ở trên); web chạy, trang chủ 200, SignalR chưa đăng nhập 401, 6 worker nền chạy không lỗi. **242/242 test qua** (99 không DB ~5 s; 143 DB theo 3 đợt ~20–40 s/đợt, gồm KhoaHocVienTests).
- **Test chưa mang (cần helper của test view/controller):** LopSoLieu, PhanTrangHocVien, RaSoatGiaoDienDot1, SecurityRound3, Tang4c, Tang5a, Tang5c1 (kiểm migration Tang5c1 → viết lại), Tang8ErrorPage, EmailLayout + 40 file dùng controller/view. Helper cần: `Tang4ViewTests`, `PlainLanguageAudit`, `TestLearnHub`, `RecordingLearnNotifier`, `SafetyIdentity`, `Tang5bPureTests`, `Tang8PureTests`.

### 2026-11-14 — Claude: bước 3, controller

- Chép 23 controller từ QuizEdu (đổi thương hiệu như bước 2; `HomeController` thay bản template). Build 0 lỗi. Thêm project test vào `MindSprintAI_ASP.NET_MVC.slnx`.
- Mang thêm `RekalDashboardTests`, `Tang2ConcurrencyTests`: **20/20 qua**. Tổng đã mang: 262 test, đều qua.
- **Chưa có view** → các trang (trừ trang chủ template) sẽ lỗi khi mở cho tới bước giao diện. 45 file test còn lại kiểm cả view/wwwroot (Razor render, PlainLanguageAudit, play.js…) — mang cùng bước giao diện. `GiangDuongTests` chờ `Tang5bPureTests` (trong Tang5bTests).
