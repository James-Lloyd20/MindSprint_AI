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

### 2026-11-14 — Claude: bước 4a, view + tài nguyên QuizEdu (nhịp 1: tương đương)

- Chép nguyên `Views/` (106 file), `TagHelpers/`, `wwwroot/` (css/js đổi thương hiệu; `rekal*.css` → `mindsprint*.css`; `img/`, `lib/` chép nguyên), `libman.json`. Giao diện **vẫn là Rekal (Acid neo-brutalism)** — nhịp 2 đổi sang giao diện nhóm.
- Mang **toàn bộ** test còn lại (+ 3 script `*.check.cjs`). Test kiểm migration lịch sử theo tên (Tang4b, 4d, 5a, 5b, 5c1, 5d1, 5d2, 5e, 6b, 7c, 7c2, 7d, 7d2, 7e, 7e2, 8_AuditLog, 8_CongTacTuOn) đổi sang kiểm trên model/mặc định/snapshot; riêng `Tang7eTests.MigrationChiThemCotCoMacDinhHoacChoPhepTrong` bỏ hẳn (luật chung ở Tang2RuleTests). `NK6` đọc `.gitignore` ở gốc repo (`Root/..`). `GiaoDienTests.KhongConFontCu…` bỏ phần `docs/design/preview.html` (không mang).
- `tools/`: mang `run-tests.ps1/.sh`, `graphify.ps1`, `loadtest/hoc-vien-khach.js`, `loadtest/phan-tich-trace.py`. **Không mang** `tools/debian/*`, `rekal-backup.sh`, `loadtest/phan-tich-log.sh` (riêng máy chủ Debian của người dùng: dịch vụ `rekal`, Cloudflare Tunnel, Google Drive).
- **Đã kiểm:** build 0 lỗi; **1720/1720 test qua** (QuizEdu 1721, bỏ 1 test migration Tang7e); 3 script Node OK; web chạy, các trang công khai 200, xem trang Đăng nhập trên Chrome.
- **Tiếp theo:** nhịp 2 đổi giao diện theo `MindSprint-AI-D-n/frontend/tokens.css` (cam/pastel, Inter tự lưu trên máy chủ, sidebar nhóm); rồi chuyển chức năng riêng của nhóm: Kanban, lịch học/streak, điểm yếu, Sổ tay AI, gia sư OCR, thư viện từ vựng hệ thống + game ghép thẻ, Tổng quan. Bản Python `backend/app` của nhóm không chuyển (trùng bản ASP.NET).

### 2026-11-14 tối — Claude: bước 4b, đổi màu/font/hình dạng sang giao diện nhóm (nhịp 2a)

- **Quy tắc làm việc mới** (người dùng gửi `PROMPT_CACH_LAM_VIEC.md`): AI **không tự commit/push**; chỉ `git add` rồi đưa lệnh commit bọc ngoặc đơn, không ghi công AI. Claude đã lỡ tự commit 5 lần trước khi đọc file → người dùng chọn giữ mốc, chỉ bỏ dòng ghi công: đã dựng lại 6 commit bằng `git commit-tree` (cây code, tác giả, thời gian giữ nguyên; kiểm diff rỗng). Nhánh dự phòng `du-phong-truoc-bo-ghi-cong` giữ lịch sử cũ, người dùng tự xóa khi thấy ổn.
- **Font:** Inter Variable 5.1.1 (Fontsource, tự lưu ở `wwwroot/lib/fonts/inter`, OFL + metadata) thay Space Grotesk (đã xóa); giữ JetBrains Mono cho mã/đồng hồ. Không tải Google Fonts/Font Awesome như frontend nhóm cũ (giữ nguyên tắc tự chủ của QuizEdu).
- **Biến màu** (`wwwroot/css/mindsprint.css`): giữ tên biến QuizEdu, đổi giá trị theo `MindSprint-AI-D-n/frontend/tokens.css`: `--acid` = cam #FF7A45, nền #F7F5FA, chữ #1A1A2E, `--blue` = chữ cam đậm #B23E13 (link), `--pink` pastel; thêm `--line` (viền mảnh), `--elev-1/2/3` (bóng mềm), `--acid-soft`, `--accent-ink`, `--pastel-a/b/c`. Chế độ tối theo nhóm (#191721/#25212F/#F5F1F9). `--ok/--warn/--bad` là nền, chữ đậm đi kèm.
- **Thay hàng loạt (sed, mọi `mindsprint*.css` + style trong view):** viền `2px solid var(--ink)` → `1px solid var(--line)`; bóng cứng `Npx Npx 0 var(--shadow)` → `var(--elev-1/2/3)`; biến viền Bootstrap `…-border-color: var(--ink)` → `var(--line)`, `…-border-width: 2px` → 1px; màu cứng `#09090B`→`#1A1A2E`, `#F3F7DC`→`#F7F5FA` (trừ `join-qr.css`: QR giữ đen/trắng thuần để dễ quét).
- **Thành phần:** nút viên thuốc (pill), nút chính cam + chữ tối + bóng cam nhẹ, nút phụ nền cam nhạt viền #F5B493; ô nhập nền #F0EDF5 bo 12px; thẻ bo 20px; hộp thông báo nền nhạt + viền màu trạng thái; mục menu đang chọn nền cam nhạt chữ cam đậm; tiêu đề `.acid-highlight` thành chữ cam đậm (không tô nền); `.eyebrow` dùng Inter thay mono.
- **Sidebar:** sát mép trái, cao hết màn hình, nền trắng, viền phải; thêm tiêu đề nhóm mục **Học tập / Tổ chức / Ứng dụng** (`li.nav-heading`, ẩn khi thu gọn/thanh trên); "Tham gia bằng mã" chuyển lên nhóm Học tập. **Mặc định mở rộng** như nhóm (QuizEdu từng chốt mặc định thu gọn ngày 09/11) — `_Layout` dùng `rail === 'collapsed'`, `theme.js` tương ứng.
- **Test:** cập nhật 4 test giao diện theo giá trị mới, giữ mục đích (font có OFL + tiếng Việt và không còn font cũ; màu thanh trình duyệt sáng/tối; bảng màu nhóm có mặt và màu Acid không còn); `SidebarPureTests` đổi câu kiểm sang `rail === 'collapsed'`. **1717/1720 ở lượt chạy đầy đủ + 3 test sửa chạy lại qua (74/74 các lớp giao diện) = 1720/1720.**
- **Thử trình duyệt (Chrome, máy):** tạo tài khoản thử `gv.lan@test.local` (mật khẩu trong `App_Data/dev-accounts.txt`, git bỏ qua) qua đăng ký → link dev → đặt mật khẩu → đăng nhập; xem Đăng nhập, Đăng ký, Bài của tôi, Tài khoản ở chế độ sáng và tối. Sửa lỗi tối: nhãn "Vai trò: Quản trị viên" mất chữ vì `--pink` tối → đổi `--pink` tối thành #C98AAE.
- **Ghi nhận chưa rõ:** trên Chrome có tiện ích chèn script (`requests.js`, `extend-native-history-api.js`), bấm Đăng nhập lần đầu **không gửi form** (`device-signals.js` gọi `requestSubmit` sau `await`; server không nhận POST), bấm lần 2 hoặc gọi `requestSubmit()` trực tiếp thì được. Code giống QuizEdu (web thật đã có 650 người đăng nhập) → nghi do môi trường; **chưa thử trên trình duyệt sạch**.
- **Còn dở (nhịp 2b, người dùng duyệt):** bố cục vẫn của QuizEdu → dựng lại khung theo nhóm: header trên cùng (tìm kiếm nhảy trang, chuông, giao diện tối, avatar + tên), sidebar (logo icon, nút chính ở đáy), bỏ chân trang trong ứng dụng, mẫu đầu trang (icon + tiêu đề + mô tả); rồi chuyển thành phần trong trang sang kiểu nhóm (thẻ pastel, dải số liệu, tab viên thuốc). Hai hình minh họa `img/landing/study-ai-*.webp` còn màu neon Rekal (ảnh bitmap, cần vẽ/chọn lại).

### 2026-11-14 khuya — Claude: bước 4c, khung giao diện theo bố cục nhóm (nhịp 2b, bước 1)

- `_Layout.cshtml`: **header ứng dụng** `.app-topbar` (chỉ người đã đăng nhập): ô "Tìm trang…" (datalist các trang trong thanh bên + menu tài khoản, chọn/Enter thì chuyển trang, không gọi máy chủ — `site.js`), chuông `#notification-bell`/`#notification-unread` chuyển từ thanh bên lên đây (`notifications.js` không đổi), menu tài khoản (avatar + tên + vai trò). Menu tài khoản viết **một lần** dạng Razor templated delegate `accountMenu`, hiện ở header (người đã đăng nhập) hoặc thanh trên (khách).
- Thanh bên: logo có icon `copy` cam; nhóm **Học tập** (Bài của tôi/Bộ học, Lớp, Tham gia bằng mã), **Tổ chức** (GV: Báo cáo), **Ứng dụng** (Thùng rác, Quản trị); đáy: nút chính (HV "Vào lớp bằng mã", GV "Bộ học mới" → `/StudySets#tao-bo-hoc`, `site.js` mở `<details id="tao-bo-hoc">`) + link Chính sách · Điều khoản. Chân trang website ẩn trong ứng dụng (vẫn hiện ở trang ngoài; phần ghi nguồn ảnh vẫn giữ).
- CSS cuối `mindsprint.css`: `.app-topbar`, `.app-search`, `.app-icon-btn` (cần `flex: none`, nếu không bị co còn 2px), `.sidebar-footer`, `.brand-icon`.
- **Test:** `RekalBatchViewTests.ThanhBenCoTenNganVaThungRacChung` bỏ "Thông báo" khỏi danh sách nhãn thanh bên, thêm kiểm chuông nằm trong header. Lượt đầy đủ: 1719/1720, 1 test trên sửa lại → lớp đó 14/14 → **1720/1720**. Lượt trước bị Claude Code dừng vì máy thiếu RAM (chạy lại khi máy còn 3,2 GB trống).
- **Thử Chrome:** Bài của tôi, Tài khoản, Thùng rác (qua ô tìm trang) ở 1920px; khung 390px (iframe) không cuộn ngang.
- **Còn dở (bước 2):** thành phần trong trang theo kiểu nhóm (đầu trang có ô icon, thẻ pastel, dải số liệu, tab viên thuốc); chế độ thanh bên thu gọn chưa xem lại; chưa thử điện thoại thật.
- 14/11 khuya: người dùng yêu cầu commit + push lên `https://github.com/James-Lloyd20/MindSprint_AI` (repo **công khai**, đã có sẵn commit "Initial commit" chỉ chứa README). Claude commit `91fdf65`, gộp README bằng merge (không force) `61e79f8`, push `main`. Nhánh `du-phong-truoc-bo-ghi-cong` chỉ ở máy. Từ nay vẫn theo quy tắc: chỉ commit/push khi người dùng yêu cầu rõ.

### 2026-11-14 khuya — Claude: bước 4d, thành phần trong trang theo nhóm (nhịp 2b, bước 2, đợt 1)

- Tiêu đề trang: `main h1` (kể cả `.h3/.h4`) cỡ clamp(1.625–2.25rem) đậm, dòng mô tả sau h1 màu xám (`mindsprint.css` cuối).
- Thẻ `.set-card/.class-card/.learn-card`: nền pastel xoay vòng A/B/C, bỏ viền, rê chuột bóng cam (`mindsprint-pages-d.css`, nạp cuối nên thắng).
- `.section-icon`: nền cam nhạt + icon cam đậm đồng nhất (`mindsprint-decor.css`; bỏ `--tint`); 4 rule "icon xám" ở pages-a/b/c đổi theo; bỏ rule ép chữ trắng khi `--tint:var(--blue)`.
- Font mono chỉ còn cho mã/đồng hồ (code, join-code, timer, mã bài giao, QR); nhãn/số đếm/chữ cái đáp án/sticky-note/sticker chuyển Inter.
- Dữ liệu thử trong DB dev: bộ học "Tiếng Anh giao tiếp", lớp "Lập trình Web – Nhóm 1"; tài khoản thử đang ở chế độ Giảng viên.
- **Đã kiểm:** Chrome — Bộ học, Lớp tôi dạy, chi tiết lớp, Báo cáo, chi tiết bộ học; **1720/1720 test qua**.
- **Còn dở:** trang làm bài, chấm lại, theo dõi trực tiếp, trang chủ khách (cần dữ liệu bài giao/học viên); chế độ tối của thẻ pastel; hình minh họa neon Rekal.

### 2026-11-15 — Claude: SỬA LỖI chọn sai giao diện nhóm (cam → xanh chanh) + đối chiếu bản nhóm đang chạy

- **Lỗi của Claude (14/11):** lấy màu từ `MindSprint-AI-D-n/frontend/tokens.css` + ảnh `design-reference/*-v39.png` (cam, 06/10) mà **không chạy bản nhóm lên xem**. Thực tế `frontend/ui-refresh.css` (02–04/11, nạp sau cùng) đè lên: **xanh chanh `#c7e977`, chữ `#202b3e`, nền `#fffefa`**, font **Manrope** (tiêu đề) + **DM Sans** (nội dung). Bản cam đã bị đẩy lên GitHub ở commit `91fdf65`/`79002ef`; bản này sửa lại. Người dùng chỉ ra; chốt chọn xanh chanh.
- **Đối chiếu bản nhóm đang chạy:** chạy backend nhóm (.NET 8 trên runtime .NET 10 bằng `DOTNET_ROLL_FORWARD=Major`, DB riêng `MindSprintGroupRef`, tài khoản demo trong DataSeeder của nhóm) + frontend ở `127.0.0.1:5500`; đi qua 9 tab, thử Kanban (thêm việc), thẻ học (lật mặt), quiz (trả lời đúng), chế độ tối, điện thoại 390px. Ảnh lưu `docs/giao-dien-nhom/` (00–18), ghi chép + danh sách chênh lệch ở **`docs/GIAO_DIEN_NHOM.md`** (có cách chạy lại). Repo nhóm không bị sửa (bin/obj bị git bỏ qua).
- **Số đo lấy bằng `getComputedStyle` từ bản nhóm** (không ước lượng bằng mắt): sidebar 260px, đệm 26/17/20; tiêu đề nhóm mục 10.56px/800/giãn .12em/#727a80; mục menu 14px/600 #536071, chọn 700 #526d31 nền #eef6d9 bo 12px cao 43px; nút 14.4px/700 cao 44px bo 13px; h1 32px/800 chữ khít -.055em, mô tả 13px; ô tìm bo 14px viền #a3aa9a rộng 510px; nút icon 44px bo 14px; avatar 44px bo 13px nền #eef6d9 viền #d8e6bb; thẻ bo 20px bóng `0 10px 30px rgba(34,42,54,.043)` đệm 24px; ô icon thẻ 44px bo 14px. Chữ nhấn thật là **#526d31** (ui-refresh ghi #55763c — còn file khác đè).
- **Đã đổi:** biến màu sáng/tối theo ui-refresh (`mindsprint.css` đầu file; thêm `--accent-border`, `--btn-main-hover`, `--btn-shadow*`, `--alert-*`, `--wash-*`, `--sidebar-bg`, `--font-heading`); font Manrope + DM Sans (DM Sans không có tiếng Việt → chữ có dấu dùng Inter, ghi trong `lib/fonts/README.md`); nền trang có vệt xanh/hồng đào; logo ô icon 40px; **thẻ người dùng** dưới logo (tên + "Đang dùng: Học viên/Giảng viên" + Đăng xuất — nhóm ghi khẩu hiệu, MVC ghi chế độ vì có 2 chế độ); tiêu đề nhóm IN HOA; avatar header vuông + email; nút phụ nền trắng viền xám; **thanh tab dưới đáy trên điện thoại** (Bài học/Bộ học · Lớp · Mã lớp/Báo cáo · Thông báo · Khác) thay nút ☰. Bỏ hết mã màu cam đã viết cứng.
- **Test:** GiaoDienTests cập nhật theo bảng màu/font mới (và cấm lại màu cam); quy tắc "không đậm 800" của QuizEdu đổi thành "không đậm 900" (nhóm dùng Manrope 800, Manrope có tới 800). `RaSoatTkAiHvTests.L6` chỉ xét dòng menu thanh bên (`nav-link`) vì thanh tab điện thoại cũng có link Lớp. Lượt đầy đủ 1719/1720, sửa L6 → lớp đó 54/54 → **1720/1720**.
- **Còn khác bản nhóm:** nút "Giao diện tối" + nút thu gọn nằm đầu sidebar (test QuizEdu bắt buộc trước menu cho điện thoại; nhóm để trong Cài đặt); trên điện thoại vẫn có thanh logo trên cùng; chưa có trang chủ bảng điều khiển, nút thư, link "Trợ giúp"; các trang nội dung (làm bài, chấm lại, theo dõi, bảng điểm) chưa so từng trang với ảnh nhóm; chế độ tối chưa so.

### 2026-11-15 — Claude: so từng trang với bản nhóm (làm bài, điện thoại, giảng viên, tối)

- **Làm bài** (`#play .options`): theo `.quiz-option-btn` của nhóm (đo getComputedStyle): lưới 2 cột cách 16px, ô đệm 16px viền mảnh bo 14px chữ 15.2px/500 căn trái, vòng chữ cái A–D 26px (CSS counter), đúng `.btn-success` nền `--muted` viền/chữ `--sage` (#7E9A67, token mới), sai `.btn-danger` viền/chữ `--bad-text`; `.feedback` có khung. **Giữ trợ năng QuizEdu:** đáp án đã chọn hiện ✓/✗ trong vòng tròn (không chỉ báo bằng màu).
- **Điện thoại:** ô tìm + chuông + avatar một hàng (ẩn tên/email ≤ 575px); thử nút "Khác" ở thanh tab mở `#app-menu` đủ mục.
- **Tiêu đề mục:** `.h1–.h6` của Bootstrap ép 500 → đặt 700 như nhóm.
- **Đã xem:** Làm bài (đúng), Bài của tôi (sáng, tối), khung 390px, Chi tiết bài giao + bảng điểm (GV có lượt làm của HV). **1720/1720 test qua.**
- **Lưu ý làm việc:** `MapStaticAssets` đóng gói wwwroot lúc build → sửa CSS phải build lại; Chrome có thể giữ CSS cũ → Ctrl+Shift+R.
- Tài khoản thử trong DB dev: `gv.lan@test.local` (Admin, đang ở chế độ Giảng viên), `hv.minh@test.local` (học viên, trong lớp "Lập trình Web – Nhóm 1", có 1 lượt làm); mật khẩu ở `App_Data/dev-accounts.txt`.

### 2026-11-15 — Claude: Công việc (Kanban) chuyển từ bản nhóm

- **Nguồn:** `MindSprint-AI-D-n/backend/MindSprint.Api` (`KanbanTask`, `TasksController`) + `frontend/kanban.js|css`. Giữ nguyên quy tắc: tiêu đề 1–200, mô tả ≤ 5000, ưu tiên Thấp/Trung bình/Cao (mặc định Trung bình), 3 cột Chưa bắt đầu/Đang làm/Hoàn thành, thứ tự trong cột 0..n-1, chuyển cột/xóa thì đánh số lại, sửa đổi cột thì về cuối cột mới.
- **DB:** `Data/Entities/Kanban.cs`, bảng `KanbanTasks` (FK người dùng, index UserId+Status+Position), migration chỉ thêm `20261114173346_Kanban`. Xóa tài khoản xóa luôn việc Kanban (`AccountDeletionService`).
- **Service:** `KanbanService` — khác bản nhóm: khóa theo người (`AttemptLock.KanbanAsync`, sp_getapplock) thay cho Serializable để nhiều tab không deadlock; hạn hoàn thành lưu là một ngày.
- **Controller/View:** `KanbanController` (Index, Create, Edit, Move, Delete; khách bị NotGuestFilter chặn), `Views/Kanban/Index.cshtml`, `wwwroot/js/kanban.js` (hộp thêm/sửa dùng chung, lọc chữ + ưu tiên, kéo thả bằng fetch), CSS cuối `mindsprint-pages-d.css` đo từ bản nhóm. Thêm/sửa/xóa/mũi tên là form thường → chạy cả khi JS lỗi. Menu: mục "Công việc" dưới TỔ CHỨC cho mọi người dùng (icon `square-kanban` mới).
- **Test:** `KanbanTests` 10 ca (quy tắc, thứ tự, người khác không đụng được, 8 tab tạo song song không trùng vị trí). Đã thử trên Chrome: thêm (nút chung + nút cột), sửa, kéo thả sang cột, nút mũi tên, tìm, lọc, chế độ tối.
- **Quyết định của người dùng 15/11:** menu + cách tổ chức theo nhóm (Trang chủ/Thư viện/Luyện tập/Lớp học/Sổ tay AI · Tổng quan/Điểm yếu/Lịch học/Công việc · Cài đặt); khách vào thẳng ứng dụng (bỏ landing kiểu QuizEdu); không chép bố cục QuizEdu. **Việc tiếp theo:** đổi menu + làm trang chủ kiểu nhóm, rồi làm lại từng trang.
- **Rà soát 15/11 — chức năng nhóm còn thiếu trong kế hoạch (phải làm, không miễn):** AI tạo bộ thẻ/quiz từ văn bản hoặc file (`AiController` extract-file/flashcards/quiz), chuông báo giờ học theo lịch (bíp/giọng nói/cả hai, màn "Tắt chuông & Vào học ngay"), PWA cài lên điện thoại + offline (`sw.js`), nói/nghe trong Sổ tay AI (`voice.js`), trang Cài đặt (cài app, sáng/tối, kiểu chuông), nút đăng nhập demo. Không chuyển: token refresh/revoke (Identity cookie thay), Flashcards/Review (QuizEdu đã có SRS), Python `backend/app` (bản cũ của API .NET). Trang gốc trên menu (gồm Công việc) không cần nút Quay lại. **1731/1731 test qua.**

### 2026-11-15 — Claude: menu + trang chủ theo nhóm

- **Menu** (`_Layout.cshtml`): HỌC TẬP Trang chủ · Thư viện (StudySets) · Luyện tập (Learn) · Lớp (giữ tên theo chế độ "Lớp tôi dạy/Lớp của tôi"); TỔ CHỨC Công việc · Báo cáo (GV); ỨNG DỤNG Thùng rác · Quản trị. "Tham gia bằng mã" rời menu, còn ở nút cuối thanh bên (học viên) và trong trang Lớp. Thanh tab điện thoại như nhóm: Hôm nay · Thư viện · Luyện tập · Công việc · Khác. Icon mới `house`.
- **Khách chưa đăng nhập** vào thẳng khung ứng dụng (thẻ "Khách" + Đăng nhập; nút cuối "Tạo tài khoản"; đầu trang Đăng nhập/Tạo tài khoản). Trang Account/* và khách vào bằng mã giữ thanh trên như cũ.
- **Trang chủ** (`HomeService` + `Views/Home/Index.cshtml`, số đo từ trang chủ nhóm): lời chào + ngày · thẻ "Hôm nay học gì?" · Tiến độ (vòng % công việc xong, thẻ cần ôn SRS, bài cần làm/bài đang mở) · Việc hôm nay (3 việc Kanban chưa xong) · Bài đang diễn ra (GV) / Bài cần làm (HV, cùng quy tắc "Cần làm" của Learn/Index) · Bộ học của bạn · Góc học tập. Chỉ số liệu thật; chuỗi ngày học/mục tiêu ngày chờ Lịch học. Học viên không còn bị chuyển sang Bài của tôi; `Home/Lecturer.cshtml` bỏ (gộp).
- **Đã xóa (trang giới thiệu QuizEdu):** nội dung landing, `home-moments.js`, `study-ai-together.webp` (+ credits.json), 160 dòng CSS `.home-*` trong `mindsprint.css`. Test landing (GiaoDienTests 5 test, RekalDashboardTests, RekalBatchViewTests) sửa sang trang mới + thêm test trang chủ học viên.

### 2026-11-15 sáng — Claude (tự chạy 4h23): khung trang, Thư viện, Lịch học

- **Khung trang** đo lại trên bản nhóm 1920px: vùng chính đệm `0 58px 42px`, nội dung tối đa 1360px; mục menu 43px cách 5px, icon #928A9C; nút thu gọn ô 36px nổi (x=sb+12, y=105); nút sáng/tối thành nút icon trên thanh trên (trang có thanh bên). Bẫy: `<header class="mindsprint-header container">` → bộ chọn phải là `.container:not(.mindsprint-header)`.
- **Nguồn ảnh** → `Home/Privacy` mục 12 `id="nguon-anh"` (người dùng chốt).
- **Thư viện** (`StudySets/Index`) bố cục nhóm: `.page-head` (dùng chung) + `nav.library-toolbar` + lưới thẻ ≥ 290px; nút Mở/Ôn thẻ; Giao bài/đổi tên/nhân bản/xóa trong "…". Trang Learn đổi tên **Luyện tập**. Bỏ eyebrow + acid-highlight ở Account, Notifications, Reports, StudySets/Details, Flashcards/Study; `main h1.mindsprint-title` 2rem.
- **Lịch học** (`StudySlot`, migration `LichHoc`, `StudyScheduleService`, `ScheduleController`, `Views/Schedule/Index`, `js/schedule.js`): quy tắc nhóm (06:00–23:00, kết thúc sau bắt đầu, 4 chủ đề, ghi chú ≤ 40, cho chồng giờ), tối đa 100 giờ học/người. **Chuỗi ngày học** `StudyStatsService` tính trên máy chủ từ `FlashcardReview` + `AnswerSubmission` (câu tự trả lời lần đầu, bỏ câu hết giờ `"{}"`) thay cho bảng `UserStudyDays` do trình duyệt khai của nhóm. Trang chủ "Tiến độ hôm nay" = Chuỗi học · Thẻ đã ôn · Độ chính xác. Test `LichHocTests` 5 ca.
- Quyết định chờ duyệt ghi ở `docs/BAO_CAO_DEM_15-11.md` mục 3.

### 2026-11-15 sáng (tiếp) — Claude: chuông, từ vựng, điểm yếu, tổng quan, AI tạo câu hỏi, sổ tay AI

- **Chuông báo giờ học** `js/alarm.js` (mọi trang khi đăng nhập) + `/Schedule/Slots`; cài đặt ở `localStorage` (`mindsprint-alarm-enabled/style`), "đã báo hôm nay" `mindsprint-alarm-done-YYYY-MM-DD` (MVC tải lại trang nên phải nhớ, khác SPA của nhóm).
- **Thư viện từ vựng** `Data/Vocab/vocab.json` (1832 thẻ trích từ `vocab_data.js` + `defaultFlashcards` của nhóm bằng Node, script `tools/vocab/extract-vocab.cjs`), `VocabCatalog` (singleton) + `VocabLibraryService.CopyTopicAsync` (chủ đề → bộ học SRS), `VocabController` Index/Topic/Game/Copy, `js/vocab-game.js`. Test `TuVungTests`.
- **Điểm yếu** `WeakAreaService` (≥ 3 lần thử, < 70%, tính trực tiếp từ FlashcardReview + AnswerSubmission), `WeakAreasController`. **Tổng quan** `OverviewService` (thành thạo = LastRating Remembered / thẻ đã học). Test `DiemYeuTests`.
- **AI tạo câu hỏi/thẻ** `GeminiClient.GenerateAsync` + `AiGenerateService` → `ExcelImportService.ToPreview` (cùng trang xem trước nhập Excel); tệp PDF bằng gói **PdfPig 0.1.9** (bản nhóm dùng, lấy từ cache NuGet). `ImportController.AiPreview`. Số dòng AI đánh từ `ExcelTemplate.FirstDataRow`. Test `AiTaoCauHoiTests`.
- **Sổ tay AI** `Notebook/NotebookSource/NotebookNote` (migration `SoTayAi`), `NotebookService` (20 nguồn, 200.000 ký tự/nguồn; hỏi đáp qua `GeminiClient.AskAsync` + `SourceGrounding`), `NotebooksController`, `js/notebook.js` (nói/nghe). **Bẫy:** quy ước cuối `AppDbContext` đặt mọi FK của entity học tập = Restrict → xóa con trước. Test `SoTayAiTests`. Chưa làm: nguồn từ URL, Gia sư OCR.
- Menu giờ: HỌC TẬP Trang chủ · Thư viện · Luyện tập · Sổ tay AI · Lớp; TỔ CHỨC Tổng quan · Điểm yếu · Lịch học · Công việc · (GV) Báo cáo; ỨNG DỤNG Thùng rác · Quản trị.
- **Gia sư OCR** `GeminiClient.ReadImageAsync` + `NotebookService.AddImageSourceAsync` (ảnh PNG/JPG/WEBP → nguồn `Image`, ảnh không lưu). **Cài đặt** `SettingsController` + `Views/Settings` (dùng chung id `alarm-style`/`alarm-toggle` với `alarm.js`). **PWA** `wwwroot/manifest.webmanifest`, `sw.js` (chỉ đệm `/css|js|lib|img/` + `offline.html`, trang luôn lấy từ mạng), `js/pwa.js`, icon `img/app/icon-192|512.jpg` (thu nhỏ từ ảnh nhóm). **Demo** `Controllers/AccountDemo.cs` (`CanUseDemo` = Development + localhost, `demo@demo.local`, không mật khẩu). Test `CaiDatPwaDemoTests`, `SoTayAiTests`.
- **Bẫy test rà ngôn ngữ (`PlainLanguageAudit`):** mọi chuỗi tiếng Việt trong Views/Services/js bị quét cả lời nhắc AI — tránh chữ JSON/API/key/server… (trừ "API key Gemini").
- Báo cáo đêm + mục cần duyệt: `docs/BAO_CAO_DEM_15-11.md`.
