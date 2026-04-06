# TROUBLESHOOTING — Sự cố thường gặp & cách xử lý

> **Last Updated**: 2026-04-06

---

## §1 — Sự cố backend

### §1.1 — Database migration thất bại

**Triệu chứng**: `dotnet ef database update` ném ra `SqlException` hoặc `SqliteException`

**Cách xử lý:**
1. **Kiểm tra provider**: Đảm bảo `DatabaseProvider` trong appsettings khớp DB đích
2. **Migrations đang chờ**: Chạy `dotnet ef migrations list` để xem migration chưa áp dụng
3. **Reset**: Xóa `vah.db` (SQLite) hoặc `docker compose down -v` (PostgreSQL)
4. **Sai discriminator**: SQL fix khi startup sẽ xử lý việc này — đảm bảo `Program.cs` chạy auto-migration

### §1.2 — JWT authentication thất bại (401)

**Triệu chứng**: Tất cả API calls trả về 401 Unauthorized

**Cách xử lý:**
1. **Kiểm tra token**: Decode tại https://jwt.io — xác minh expiry, issuer, audience
2. **Sai cấu hình**: `Jwt:SecretKey`, `Jwt:Issuer`, `Jwt:Audience` phải khớp giữa tạo token và validate
3. **ClockSkew = Zero**: Thời gian server phải chính xác — không chấp nhận clock drift
4. **SignalR**: Đảm bảo token được gửi qua query param `?access_token=`, không phải header

### §1.3 — File upload thất bại

**Triệu chứng**: 413 Payload Too Large hoặc 400 Bad Request khi upload

**Cách xử lý:**
1. **Giới hạn Kestrel**: `Program.cs` đặt 100 MB — kiểm tra file có vượt mức này không
2. **FileUploadConfig**: Tối đa 50 MB mỗi file, 20 files mỗi request
3. **Rate limit**: Upload policy cho phép 20/phút — kiểm tra có bị throttled không
4. **Extension**: Xác minh file extension có nằm trong `FileUploadConfig.AllowedExtensions`
5. **Dung lượng đĩa**: Kiểm tra `wwwroot/uploads/` còn đủ storage

### §1.4 — SignalR connection thất bại

**Triệu chứng**: Cập nhật realtime không hoạt động, console hiển thị lỗi WebSocket

**Cách xử lý:**
1. **CORS**: Đảm bảo đã bật `AllowCredentials()` (bắt buộc cho SignalR)
2. **Nginx proxy**: Headers upgrade của WebSocket phải được forward
3. **Auth**: Token phải hợp lệ khi kết nối được thiết lập
4. **Reconnect**: `withAutomaticReconnect()` nên được cấu hình ở frontend

### §1.5 — Tạo thumbnail thất bại

**Triệu chứng**: Ảnh upload thành công nhưng thumbnail bị null

**Cách xử lý:**
1. **ImageSharp**: Xác minh đã cài SixLabors.ImageSharp 3.1.12
2. **Quyền ghi**: Thư mục `wwwroot/uploads/thumbs/` phải cho phép ghi
3. **Định dạng**: ImageSharp hỗ trợ JPEG, PNG, GIF, WebP, BMP — không hỗ trợ SVG
4. **Bộ nhớ**: Ảnh lớn có thể OOM — kiểm tra giới hạn bộ nhớ của Kestrel

## §2 — Sự cố frontend

### §2.1 — Lỗi CORS

**Triệu chứng**: Browser console hiển thị lỗi `Access-Control-Allow-Origin`

**Cách xử lý:**
1. **Origin**: URL frontend phải nằm trong `Cors:AllowedOrigins` (mặc định: `localhost:5173,5174`)
2. **Credentials**: `AllowCredentials()` là bắt buộc cho SignalR — không tương thích với `AllowAnyOrigin()`
3. **Docker**: Khi dùng Docker, frontend ở port 3000 — đảm bảo nó có trong allowed origins

### §2.2 — API calls trả về network error

**Triệu chứng**: Axios ném `ERR_NETWORK` hoặc `ERR_CONNECTION_REFUSED`

**Cách xử lý:**
1. **Backend đang chạy**: Xác minh backend hoạt động tại `http://localhost:5027/api/v1/health`
2. **Vite proxy**: Kiểm tra cấu hình proxy trong `vite.config.js` cho API forwarding
3. **Docker networking**: Services giao tiếp qua tên container, không phải `localhost`

### §2.3 — State không cập nhật sau thao tác

**Triệu chứng**: UI không phản ánh thay đổi sau các thao tác CRUD

**Cách xử lý:**
1. **SignalR bị ngắt**: Kiểm tra trạng thái kết nối của hook `useSignalR`
2. **Thiếu handler**: Xác minh event type đã được đăng ký trong `AppContext.signalRHandlers`
3. **Stale closure**: Đảm bảo `refreshItems` nằm trong dependency array của hook
4. **Cache**: Kiểm tra `IDistributedCache` có đang phục vụ dữ liệu cũ không

## §3 — Sự cố Docker

### §3.1 — Container không khởi động được

```bash
# Kiểm tra logs
docker compose logs backend

# Cách khắc phục thường gặp
docker compose down
docker compose build --no-cache
docker compose up -d
```

### §3.2 — Kết nối database bị từ chối

**Triệu chứng**: Backend không kết nối được PostgreSQL khi khởi động

**Cách xử lý:**
1. **Thứ tự khởi động**: Backend phụ thuộc PostgreSQL — Docker Compose xử lý việc này nhưng startup ban đầu có thể race
2. **Connection string**: Xác minh `Host=postgres` (tên container, không phải `localhost`)
3. **Volume**: Nếu DB volume bị hỏng, chạy `docker compose down -v` rồi tạo lại

### §3.3 — Vấn đề kết nối Redis

**Triệu chứng**: Có cảnh báo về cache trong logs

**Không nghiêm trọng**: Backend sẽ fallback sang `DistributedMemoryCache` nếu Redis không khả dụng. Ứng dụng vẫn chạy không có Redis, nhưng sẽ không có distributed caching.

---

> **Document End**
