# Triển khai MindSprint AI lên server cá nhân (Linux + Docker + Cloudflare Tunnel)

Mô hình: **Trình duyệt → Cloudflare → Cloudflare Tunnel (cloudflared trên server) → `http://localhost:8080` → container `mindsprint` → container SQL Server.**

> **Cổng 80/443 là gì?** Là "cửa" mặc định của web (80 = http, 443 = https). Vì dùng **Cloudflare Tunnel**, server tự kết nối ra Cloudflare nên **không cần mở cửa nào** trên server; Cloudflare lo phần https. App chỉ mở `127.0.0.1:8080` cho chính server.

## 0. Chuẩn bị (làm trên trình duyệt, một lần)

| Cần | Ở đâu | Dùng cho |
|---|---|---|
| **Turnstile** Site key + Secret key | Cloudflare dashboard → **Turnstile** → *Add widget* → nhập tên miền → *Managed* | Hộp chống robot ở trang đăng ký/đăng nhập (bắt buộc khi chạy thật) |
| **Mật khẩu ứng dụng Gmail** | Bật xác minh 2 bước cho Gmail → https://myaccount.google.com/apppasswords → tạo mật khẩu 16 ký tự | Gửi thư xác nhận email (thiếu thì người đăng ký mới không đăng nhập được) |
| Google Client ID/Secret (không bắt buộc) | `docs/HUONG_DAN_DANG_NHAP_GOOGLE.md` | Nút đăng nhập Google |

## 1. Lấy code lên server

```bash
git clone <địa chỉ repo nhóm> mindsprint && cd mindsprint      # lần đầu
# hoặc: cd mindsprint && git pull                               # các lần sau
```

## 2. Tìm thông tin container SQL Server

```bash
docker ps --format '{{.Names}}\t{{.Image}}\t{{.Ports}}'          # tên container SQL Server (cột 1)
docker inspect <tên-container-sql> --format '{{json .NetworkSettings.Networks}}'   # tên mạng (khóa đầu tiên)
docker network inspect <tên-mạng> --format '{{(index .IPAM.Config 0).Gateway}}'   # gateway, ví dụ 172.18.0.1
```

Nếu tên mạng là `bridge` (mạng mặc định), tạo mạng riêng rồi nối SQL Server vào: `docker network create mindsprint-net && docker network connect mindsprint-net <tên-container-sql>`, sau đó dùng `mindsprint-net` cho các bước dưới.

## 3. Điền cấu hình

```bash
cd deploy
cp .env.example .env
chmod 600 .env          # chỉ chủ tài khoản đọc được (có mật khẩu)
nano .env
```

Điền: `SQL_NETWORK` (tên mạng), tên miền ở 3 dòng `App__BaseUrl` / `AllowedHosts` / `Passkeys__ServerDomain`, `ConnectionStrings__Default` (tên container SQL + mật khẩu), `ReverseProxy__KnownProxies__0` (gateway ở bước 2), Turnstile, SMTP, `Admin__Emails__0` (email của bạn để vào trang Quản trị). Mật khẩu có ký tự `$` thì viết `$$`.

## 4. Đóng gói và cập nhật database

```bash
docker compose build
docker compose run --rm --entrypoint sh mindsprint -c './efbundle --connection "$ConnectionStrings__Default"'
```

Lần đầu lệnh thứ hai tạo database `MindSprintAI` và mọi bảng; các lần sau chỉ thêm phần mới. Thấy `Done.` là được.

## 5. Chạy

```bash
docker compose up -d
curl -I http://127.0.0.1:8080          # thấy HTTP/1.1 200 hoặc 302 là app đã chạy
docker logs mindsprint --tail 50       # xem lỗi nếu có
```

App tự khởi động lại khi server khởi động lại (`restart: unless-stopped`).

## 6. Nối tên miền qua Cloudflare Tunnel

Cloudflare dashboard → **Zero Trust → Networks → Tunnels** → chọn tunnel của server → **Public Hostname → Add**:
- *Subdomain/Domain*: tên miền của app
- *Service*: `HTTP` — `localhost:8080`

Mở `https://<tên-miền>` trên điện thoại và máy tính.

## 7. Thử sau khi lên mạng

1. Đăng ký tài khoản mới → nhận thư xác nhận → đăng nhập.
2. Dán API key Gemini ở trang Tài khoản, hỏi thử Sổ tay AI.
3. Trên điện thoại: **Cài đặt → Cài ứng dụng lên máy**; mở Ôn thẻ, tắt Wi-Fi giữa chừng, ôn tiếp rồi bật lại mạng (đánh giá được gửi bù).
4. Nút "Dùng thử tài khoản demo" **không** có trên server (chỉ hiện khi chạy trên máy dev).

## 8. Cập nhật phiên bản mới

```bash
cd mindsprint && git pull && cd deploy
docker compose build
docker compose run --rm --entrypoint sh mindsprint -c './efbundle --connection "$ConnectionStrings__Default"'
docker compose up -d
```

## 9. Sao lưu

- **Database:** sao lưu database `MindSprintAI` trong container SQL Server như các database khác.
- **Ổ dữ liệu `mindsprint-data`** (khóa mã hóa key Gemini và phiên đăng nhập, nhật ký): `docker run --rm -v deploy_mindsprint-data:/d -v "$PWD":/b alpine tar czf /b/mindsprint-data.tgz -C /d .` (tên ổ xem bằng `docker volume ls`). Mất ổ này thì mọi người phải đăng nhập lại và dán lại key Gemini.

## 10. Lỗi thường gặp

| Hiện tượng | Nguyên nhân | Cách sửa |
|---|---|---|
| Trình duyệt báo "chuyển hướng quá nhiều lần" | App không tin Cloudflare Tunnel nên tưởng đang là http | `ReverseProxy__KnownProxies__0` phải đúng gateway ở bước 2; `docker compose up -d` lại |
| Container dừng ngay, log có "App:BaseUrl bắt buộc…" hoặc "Turnstile đang trống…" | Thiếu cấu hình bắt buộc khi chạy thật | Điền đủ trong `.env` |
| Mở tên miền thấy lỗi 400 (Bad Request) | `AllowedHosts` khác tên miền đang dùng | Sửa `AllowedHosts` cho đúng tên miền |
| Log có "Login failed" / "server was not found" | Sai mật khẩu SQL hoặc app không cùng mạng với SQL | Kiểm `ConnectionStrings__Default`, `SQL_NETWORK` |
| Người đăng ký không nhận được thư | Thiếu/sai SMTP | Kiểm `Smtp__*`; log có dòng "Chưa cấu hình SMTP" |
| Google báo `redirect_uri_mismatch` | Chưa thêm địa chỉ server | Thêm `https://<tên-miền>/signin-google` ở Google Cloud |
