# Visual Asset Hub — Hướng dẫn Triển khai & Vận hành

> Cập nhật lần cuối: 01/03/2026
> Thuật ngữ dùng chung: `docs/GLOSSARY.md`

---

## 1. Yêu cầu môi trường

## 1.1 Bắt buộc

- .NET SDK 9+
- Node.js 18+ (khuyến nghị 22)
- npm 9+

## 1.2 Tùy chọn

- Docker + Docker Compose (để chạy dạng production-like)

---

## 2. Chạy local

## 2.1 Backend

```bash
cd VAH.Backend
dotnet restore
dotnet build
dotnet run
```

URL mặc định:

- API: http://localhost:5027
- Swagger: http://localhost:5027/swagger
- Health: http://localhost:5027/api/v1/health

## 2.2 Frontend

```bash
cd VAH.Frontend
npm install
npm run dev
```

URL mặc định:

- App: http://localhost:5173

---

## 3. Chạy bằng Docker Compose

```bash
docker compose up --build -d
```

Service:

- Frontend: http://localhost:3000
- Backend: http://localhost:5027
- PostgreSQL: localhost:5432
- Redis: localhost:6379

Dừng và dọn:

```bash
docker compose down
docker compose down -v
```

---

## 4. Nghiệp vụ vận hành thường gặp

## 4.1 Build kiểm tra nhanh

```bash
cd VAH.Backend
dotnet build VAH.Backend.csproj
```

## 4.2 Migration

```bash
cd VAH.Backend
dotnet ef migrations add <TenMigration>
dotnet ef database update
```

Lưu ý: backend hiện auto-apply migration khi khởi động.

## 4.3 Đổi provider DB

- SQLite: `DatabaseProvider=SQLite`
- PostgreSQL: `DatabaseProvider=PostgreSQL`

Đảm bảo cập nhật đúng `ConnectionStrings:DefaultConnection`.

---

## 5. Checklist sau refactor lớn

- Build backend thành công
- Swagger truy cập được
- Health endpoint phản hồi đúng
- Đăng nhập JWT hoạt động
- Upload hoạt động
- Duplicate in-place và duplicate target-folder hoạt động
- Tags, Search, Collections, Permissions hoạt động

---

## 6. Khắc phục sự cố

## 6.1 Lỗi file exe bị lock khi build

Triệu chứng:

- `MSB3021` hoặc `MSB3027`

Cách xử lý:

- dừng process backend đang chạy
- build lại

## 6.2 401 Unauthorized

Kiểm tra:

- token có gửi đúng không
- token còn hạn không
- issuer/audience/signing key khớp config không

## 6.3 Lỗi upload

Kiểm tra:

- request là multipart/form-data
- có file hợp lệ
- file size và extension đúng cấu hình
- collection/folder id có quyền truy cập

## 6.4 Lỗi SignalR

Kiểm tra:

- backend chạy ổn định
- CORS origin cho phép
- access_token được truyền đúng khi negotiate websocket

---

## 7. Nguyên tắc an toàn kiến trúc khi phát triển Assets

1. Đặt request contracts trong `Features/Assets/Contracts`.
2. Controller chỉ điều phối, không chứa nghiệp vụ sâu.
3. Không mở stream trực tiếp trong controller.
4. Mọi logic nghiệp vụ qua `IAssetApplicationService`.
5. UserId lấy qua `IUserContextProvider`.
6. Route names dùng `AssetRouteNames`.
