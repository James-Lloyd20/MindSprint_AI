# Font nội bộ của MindSprint AI

- **@fontsource-variable/inter@5.1.1**: Latin, Latin mở rộng, Vietnamese; trục độ đậm 100–900, WOFF2 (`inter/`, giấy phép SIL OFL 1.1 trong `inter/OFL.txt`, metadata.json gốc). Font chính của giao diện nhóm (`MindSprint-AI-D-n/frontend/tokens.css`).
- **@fontsource/jetbrains-mono@5.3.0**: Latin + Vietnamese, 400/500/700, WOFF2 (mã, mã lớp, đồng hồ).
- Nguồn ghim phiên bản: https://cdn.jsdelivr.net/npm/@fontsource-variable/inter@5.1.1/ và https://cdn.jsdelivr.net/npm/@fontsource/jetbrains-mono@5.3.0/.
- Font được phục vụ từ dự án; không tải từ bên thứ ba khi chạy (giao diện nhóm cũ tải Google Fonts — đã thay). Khai báo từng bộ ký tự bằng unicode-range và font-display: swap ở `css/mindsprint-fonts.css`.
