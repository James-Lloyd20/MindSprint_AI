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
