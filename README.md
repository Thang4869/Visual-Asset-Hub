# Visual Asset Hub (VAH)

Ứng dụng web quản lý tài nguyên số (ảnh, link, bảng màu) với giao diện dark theme hiện đại, hỗ trợ kéo thả, tổ chức theo collection và thư mục phân cấp.

![Tech Stack](https://img.shields.io/badge/.NET-9.0-512BD4?style=flat&logo=dotnet)
![React](https://img.shields.io/badge/React-19-61DAFB?style=flat&logo=react)
![SQLite](https://img.shields.io/badge/SQLite-3-003B57?style=flat&logo=sqlite)
![Vite](https://img.shields.io/badge/Vite-7-646CFF?style=flat&logo=vite)

---

## Yêu cầu hệ thống

| Phần mềm | Phiên bản tối thiểu | Kiểm tra |
| --- | --- | --- |
| **.NET SDK** | 9.0 | `dotnet --version` |
| **Node.js** | 18+ (khuyến nghị 20+) | `node --version` |
| **npm** | 9+ (đi kèm Node.js) | `npm --version` |
| **Git** | bất kỳ | `git --version` |

> **Hệ điều hành:** Windows, macOS, hoặc Linux đều được hỗ trợ.

---

## Cài đặt nhanh

### 1. Clone repository

```bash
git clone <https://github.com/Thang4869/Visual-Asset-Hub.git>
cd 1A
```

### 2. Chạy Backend

```bash
cd VAH.Backend
dotnet restore
dotnet run
```

Backend khởi động tại: **http://localhost:5027**

- Database SQLite (`vah_database.db`) tự động được tạo khi chạy lần đầu.
- 3 collection mặc định (Images, Links, Colors) được seed sẵn.
- Swagger UI (API docs) có sẵn tại: http://localhost:5027/swagger

### 3. Chạy Frontend

Mở **terminal mới** (giữ backend đang chạy):

```bash
cd VAH.Frontend
npm install
npm run dev
```

Frontend khởi động tại: **http://localhost:5173**

### 4. Mở trình duyệt

Truy cập **http://localhost:5173** — ứng dụng sẽ tự kết nối đến backend.

---

## Cấu trúc dự án

```text
1A/
├── VAH.sln                          # Solution file (.NET)
├── README.md                        # File này
├── docs/                            # Tài liệu dự án
│   ├── PROJECT_DOCUMENTATION.md     # Mô tả chi tiết kiến trúc & tính năng
│   ├── ARCHITECTURE_REVIEW.md       # Đánh giá kiến trúc & roadmap nâng cấp
│   └── IMPLEMENTATION_GUIDE.md      # Hướng dẫn triển khai canvas feature
│
├── VAH.Backend/                     # ASP.NET Core 9.0 Web API
│   ├── Controllers/                 # API endpoints (Assets, Collections, Search, Health)
│   ├── Services/                    # Business logic layer
│   ├── Models/                      # Entity models & DTOs
│   ├── Data/                        # EF Core DbContext
│   ├── Middleware/                   # Exception handling
│   └── wwwroot/uploads/             # Thư mục lưu file upload
│
└── VAH.Frontend/                    # React 19 + Vite 7 SPA
    └── src/
        ├── components/              # 9 UI components
        ├── hooks/                   # Custom hooks (useAssets, useCollections)
        └── api/                     # Axios API client layer
```

---

## Tính năng chính

| # | Tính năng | Mô tả |
| --- | --- | --- |
| 1 | **Quản lý Collection** | Tạo, xóa, cây phân cấp cha/con với 4 loại: image, link, color, default |
| 2 | **Upload file** | Kéo thả hoặc chọn file, upload multipart, tên file GUID tránh trùng |
| 3 | **Thư mục lồng nhau** | Tổ chức file trong thư mục con, breadcrumb navigation |
| 4 | **3 chế độ hiển thị** | Grid, List, Masonry + Canvas kéo thả tự do |
| 5 | **Bảng màu** | Quản lý mẫu màu theo nhóm, nhập bằng mã hex |
| 6 | **Bookmark/Link** | Lưu trữ và tổ chức links |
| 7 | **Kéo & thả** | Di chuyển file giữa thư mục, sắp xếp lại thứ tự |
| 8 | **Tìm kiếm** | Server-side search theo tên + tags, phân trang |
| 9 | **Panel chi tiết** | Sidebar phải hiển thị preview + metadata khi chọn asset |

---

## API Endpoints

Backend cung cấp 19 endpoints, Swagger UI có đầy đủ tài liệu tại `/swagger` khi chạy development.

| Controller | Base route | Endpoints |
| --- | --- | --- |
| Assets | `/api/Assets` | 12 (CRUD, upload, reorder, position, folder, color, link) |
| Collections | `/api/Collections` | 5 (CRUD, items with hierarchy) |
| Search | `/api/search` | 1 (tìm kiếm assets + collections) |
| Health | `/api/health` | 1 (kiểm tra trạng thái hệ thống) |

---

## Cấu hình

### Backend (`VAH.Backend/appsettings.json`)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=vah_database.db"
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:5173", "http://localhost:5174"]
  }
}
```

### Frontend (biến môi trường)

Tạo file `.env` trong `VAH.Frontend/` nếu cần đổi URL backend:

```env
VITE_API_URL=http://localhost:5027/api
VITE_STATIC_URL=http://localhost:5027
```

> Mặc định không cần tạo `.env` — frontend tự kết nối đến `http://localhost:5027`.

---

## Xử lý sự cố (Troubleshooting)

### Backend không khởi động được

```bash
# Kiểm tra .NET SDK đã cài chưa
dotnet --version

# Nếu thiếu, tải tại: https://dotnet.microsoft.com/download/dotnet/9.0

# Restore lại dependencies
cd VAH.Backend
dotnet restore
```

### Frontend báo lỗi kết nối API

- Đảm bảo backend đang chạy tại port `5027`
- Kiểm tra CORS: origin `http://localhost:5173` phải nằm trong `appsettings.json` → `Cors.AllowedOrigins`

### Ảnh không hiển thị

- Kiểm tra thư mục `VAH.Backend/wwwroot/uploads/` tồn tại
- Đảm bảo backend đang chạy (ảnh được serve qua `http://localhost:5027/uploads/...`)

### Muốn reset database

```bash
cd VAH.Backend
# Xóa file database (Windows PowerShell)
Remove-Item vah_database.db -ErrorAction SilentlyContinue
# Chạy lại backend — DB tự tạo mới với seed data
dotnet run
```

---

## Scripts có sẵn

### Backend

```bash
dotnet run                        # Chạy development server
dotnet run --configuration Release  # Chạy release mode
dotnet build                      # Build project
```

### Frontend

```bash
npm run dev       # Chạy dev server (HMR)
npm run build     # Build production bundle
npm run preview   # Preview bản build
npm run lint      # Kiểm tra ESLint
```

---

## Tài liệu thêm

- [PROJECT_DOCUMENTATION.md](docs/PROJECT_DOCUMENTATION.md) — Mô tả chi tiết toàn bộ kiến trúc, models, API, components
- [ARCHITECTURE_REVIEW.md](docs/ARCHITECTURE_REVIEW.md) — Đánh giá kiến trúc, phân tích rủi ro, roadmap nâng cấp
- [IMPLEMENTATION_GUIDE.md](docs/IMPLEMENTATION_GUIDE.md) — Hướng dẫn tính năng Canvas kéo thả
