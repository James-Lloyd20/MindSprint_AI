# MindSprintAI_ASP.NET_MVC — hướng dẫn cho AI

Project chính của nhóm MindSprint. Logic, database và quy tắc nghiệp vụ lấy từ QuizEduManager_MVC (`../Project_Tham_Khao/QuizEduManager_MVC`); **giao diện dùng của nhóm** (`../MindSprint-AI-D-n/frontend`), không dùng giao diện QuizEdu.

Đọc `docs/AI_HANDOFF.md` trước khi làm; xong việc thì ghi thêm nhật ký (thay đổi thật, kết quả kiểm tra, phần còn dở).

- ASP.NET Core MVC (.NET 10), C#, Razor Views, HTML/CSS/JS; frontend và backend chung một project. SQL Server localhost, EF Core, ASP.NET Core Identity.
- Thứ tự làm: **Database → quy tắc nghiệp vụ trong service → controller → view**. Migration chỉ **thêm** bảng/cột (cột mới có mặc định hoặc cho phép rỗng), không xóa/đổi tên. View không tự chấm điểm hay kiểm quyền.
- Chữ trên trang cho người không rành máy: không dùng cookie, IP, token, session…; buộc dùng thì giải thích trong ngoặc.
- Không đổi quy tắc nghiệp vụ (ngưỡng, giới hạn, cách chấm) khi chưa hỏi người dùng.
- Trước khi mang/sửa phần khóa dữ liệu hay đường đi của học viên (vào bài, mở/lưu câu, tín hiệu giám sát): đọc `../Project_Tham_Khao/QuizEduManager_MVC/docs/BAI_HOC_TOI_UU_THU_TAI.md`. DB phải bật RCSI (migration `BatRcsi`).
- Cách làm: ponytail lite — thay đổi nhỏ nhất giải quyết trọn vấn đề; cuối câu trả lời ghi 1–2 dòng phần chưa kiểm chứng.
- Graphify: `graphify query "<câu hỏi>"` trước khi tìm trong code; sau khi sửa code chạy `graphify update .`. `Program.cs` và nhiều `.cshtml` không có trong graph, đọc trực tiếp.
