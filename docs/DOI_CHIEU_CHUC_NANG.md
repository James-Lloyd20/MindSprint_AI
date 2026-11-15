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
