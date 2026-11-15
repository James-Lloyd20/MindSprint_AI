# Đối chiếu chức năng: bản nhóm cũ (MindSprint-AI-D-n) → project nhóm mới

Rà ngày 15/11/2026 bằng cách liệt kê mọi tab, hộp thoại, nút (`frontend/index.html`), mã JS (`script.js`, `notebook.js`, `ui.js`…) và API (`backend/MindSprint.Api/Controllers`) của bản nhóm, rồi đánh dấu từng mục.
QuizEdu (Project_Tham_Khao): đã chuyển đủ — model DB giống hệt, toàn bộ 1720 test gốc chạy qua; không liệt kê lại ở đây.

Ký hiệu: ✅ đã có · 🟡 có nhưng thiếu/khác · ❌ chưa có

## Khung ứng dụng
| Mục nhóm | Trạng thái | Ghi chú |
|---|---|---|
| Thanh bên: hồ sơ, Học tập / Tổ chức / Ứng dụng, nút "Bộ thẻ mới", nút thu gọn | ✅ | số đo lấy từ bản nhóm đang chạy |
| Danh mục con dưới "Thư viện" (Tất cả · Tiếng Anh · Lập trình · Kiến thức chung) | ✅ | hiện khi đang ở Thư viện/Từ vựng, thêm "Bộ học của tôi" |
| Link "Trợ giúp" cuối thanh bên | ✅ | bản nhóm chỉ là dòng chữ; bên mới mở trang hướng dẫn `/tro-giup` |
| Đầu trang: ô tìm, nút thư (mở Sổ tay AI), chuông, tài khoản | ✅ | có thêm nút sáng/tối |
| Hộp thông báo chung (app-dialog) | ✅ | dùng hộp hỏi lại `data-confirm` có sẵn |

## Trang
| Tab nhóm | Trạng thái | Còn thiếu |
|---|---|---|
| Trang chủ | ✅ | "Mục tiêu hôm nay" 3 ô tích như nhóm, **tự tích** khi đã ôn thẻ / làm quiz / ghi chú hôm nay; vòng tiến độ = % mục tiêu; "Bắt đầu học 15 phút" khi chưa có bài |
| Thư viện bộ thẻ | ✅ | nút "Tạo thẻ bằng AI" trên trang Thư viện (bộ học mới chỉ tạo khi bấm Lưu); thêm/sửa thẻ ở trang Thẻ của bộ học |
| Học thẻ (lật thẻ, Nhớ/Quên, trước/sau) | ✅ | bộ học riêng: trang Ôn thẻ (lặp lại ngắt quãng, QuizEdu). Từ vựng hệ thống: hộp "Ôn tập bộ thẻ" giống ảnh 14–15 (thanh tiến trình, lật thẻ, Chưa thuộc/Đã thuộc, trước/sau) |
| Luyện tập: Trắc nghiệm & Tự luận, Game ghép thẻ | ✅ | |
| Sổ tay AI — nguồn văn bản/tệp/ảnh, hỏi đáp, ghi chú, nói/nghe | ✅ | |
| Sổ tay AI — **nguồn từ đường link** | ✅ | chặn link nội bộ/máy chủ riêng, tối đa 2 MB chữ |
| Sổ tay AI — **Studio**: Tóm tắt, Study guide, FAQ, Timeline, Mind map, Flashcard, Trắc nghiệm, Audio overview | ✅ | đủ 8 loại; nghe tổng quan đọc bằng giọng của trình duyệt |
| Sổ tay AI — **Gia sư**: Giải bài / Kiểm tra bài làm / Tự luyện / Lập trình; Từng bước / Ngắn gọn | ✅ | + đọc chữ từ ảnh đề |
| Sổ tay AI — gợi ý câu hỏi, lưu lịch sử hỏi đáp | ✅ | lịch sử lưu ở trình duyệt như nhóm |
| Hiển thị **công thức toán (KaTeX)** trong câu trả lời | ✅ | KaTeX 0.18.7 (giấy phép MIT) đặt trong `wwwroot/lib/katex` |
| Tổng quan, Điểm yếu | ✅ | "Tạo thẻ ôn tập từ điểm yếu": thẻ "Chưa nhớ" + câu làm sai → bộ học mới (không cần AI). Bài kiểm tra chỉ tính khi đã công bố đáp án |
| Lịch học + chuông báo | ✅ | |
| Công việc (Kanban) | ✅ | |
| Cài đặt | ✅ | |
| **Màn ăn mừng mốc chuỗi ngày học** (30, 50, 100… ngày) | ✅ | mỗi mốc hiện một lần |
| Nút thử của nhà phát triển (mở khóa, thử chuỗi) | — | không chuyển (công cụ thử của nhóm) |

## Kết quả rà lần cuối (15/11)
Đã đối chiếu lại toàn bộ 9 tab (`data-tab`), 10 hộp thoại (thêm/sửa thẻ, tạo thẻ AI, ôn bộ thẻ, mốc chuỗi, lịch học, công việc, chuông báo, hộp hỏi chung) và 6 nút có id của `index.html` nhóm: **không còn mục ❌**.
Khác biệt có chủ ý: Điểm yếu tính ngay khi mở trang nên không cần nút "Phân tích lại"; nút thử của nhà phát triển không chuyển.
Thêm cho buổi demo: dữ liệu mẫu khi bấm "Dùng thử tài khoản demo", `README.md`, `docs/KICH_BAN_DEMO.md`.

## Rà lần 3 (15/11, chiều) — đối chiếu từ mã nguồn, không dựa vào bảng cũ
Nguồn đã đối chiếu: 201 action + 107 view + toàn bộ service/JS của QuizEdu; toàn bộ endpoint backend nhóm (bản chính ASP.NET) và bản FastAPI phụ (`backend/app`, `standalone.html`); mọi hàm trong `script.js`, `notebook.js`, `ui.js`, `auth.js`, `voice.js`, `kanban.js`, `cockpit.js`; yêu cầu FR-01…FR-23 (`docs/requirements`); bảng phân công `YeuCauNhom/Phân Công Nhiệm Vụ`.

| Phát hiện | Nguồn | Đã làm |
|---|---|---|
| Trang chủ giảng viên thiếu "Cần xem" từng lớp (link + loại việc), chế độ + hạn bài, lối tắt "Tạo lớp"; "Giao bài" trỏ sai | QuizEdu `Home/Lecturer` | Bù đủ trong trang chủ nhóm |
| Mất nút "Nghe câu trả lời" (do viết lại notebook.js) | FR-16, voice.js | Nút Nghe/Dừng ở mọi câu trả lời hỏi đáp + Gia sư; đổi chế độ/rời trang thì dừng |
| Ghi chú mất xuống dòng/thụt dòng | FR-13 | Giữ nguyên (cả dòng đầu) |
| Studio Flashcard/Trắc nghiệm không làm thử tại chỗ | notebook.js quizHtml/pickAnswer | Làm thử tại chỗ (chấm, giải thích, tổng điểm; lật thẻ) + "Lưu thành bộ học" không gọi AI lần hai |
| Không có key / AI lỗi thì không tạo được thẻ | script.js extractFallbackCards | Tự tách thẻ theo quy tắc của nhóm (`FallbackCards`), có thông báo |
| PDF đọc sai ký hiệu toán (£ ≥ ¥ Dx x!"#) | AiController CleanExtractedText | `PdfSymbols` (chỉ cho chữ từ PDF) |
| Không tự gợi ý số thẻ | estimateOptimalCardCount | `ai-count.js` |
| Không có học bù khi lỡ giờ học | checkMakeupStudyAlert | Hộp "Bạn đã lỡ giờ học → Học bù ngay" |
| Không dán được nhiều thẻ một lúc | standalone + flashcards_bulk.py | Trang Soạn thẻ + trang Nhập |
| Ô tìm chỉ tìm trang | ui.js "Tìm trang hoặc bộ thẻ" | Tìm cả bộ học + chủ đề từ vựng |
| Không có nút sửa trên thẻ đang ôn | ảnh 14 nhóm | "Sửa thẻ" mở đúng thẻ ở Soạn thẻ |
| Lật thẻ không có hiệu ứng 3D | phân công 5B | `MindSprintFlip` (tắt khi giảm chuyển động) |
| Không chặn tệp quá cỡ/sai loại trước khi gửi | phân công 5B | `data-max-bytes` + kiểm loại theo `accept` |
| Chế độ tối: nút "Bắt đầu ngay" 1,7:1; chữ đỏ giao diện sáng 4,3:1 | đo WCAG 25 trang | đều ≥ 4,5:1 |

Có ở nhóm nhưng **cố ý không chuyển**: số "phút học" (nhóm cộng 1 phút mỗi thẻ — số giả); bật/tắt lịch (API có, giao diện nhóm không gọi; GAP-04 của nhóm); `.doc/.docx` (máy chủ nhóm từ chối); job tự tạo task ôn từ câu sai (nhóm chưa làm); AI lập kế hoạch FR-23 (nhóm chưa làm, GAP-01).
**Cần người dùng quyết:** học khi mất mạng FR-21/22 (xem báo cáo mục 9).
