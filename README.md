# Visual Asset Hub (VAH)

**Digital Asset Management** — Ứng dụng web quản lý tài nguyên số với kiến trúc modular-monolith và Assets vertical slice.

![.NET 9](https://img.shields.io/badge/.NET-9.0-purple)
![React 19](https://img.shields.io/badge/React-19.2-blue)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-17-blue)
![Redis](https://img.shields.io/badge/Redis-7-red)
![Docker](https://img.shields.io/badge/Docker-Compose-blue)
![SignalR](https://img.shields.io/badge/SignalR-10.0-green)

---

## Tính năng chính

- **Asset Management** — Upload, tổ chức, tìm kiếm files/links/colors
- **Collection Tree** — Phân cấp parent-child, 3 default collections
- **Smart Collections** — 8+ bộ sưu tập ảo tự động (recent, by type, by tag, untagged...)
- **Tags** — Hệ thống many-to-many, autocomplete, migrate legacy
- **Thumbnails** — Auto-generate 3 sizes (WebP), ImageSharp pipeline
- **Real-time Sync** — SignalR WebSocket, multi-tab, auto-reconnect
- **Multi-select** — Ctrl+click, Shift+click, bulk delete/move
- **Undo/Redo** — Ctrl+Z / Ctrl+Shift+Z, max 50 history
- **Drag Canvas** — Free positioning trên infinite canvas
- **Color Board** — Tạo và quản lý bảng màu
- **RBAC** — Owner/Editor/Viewer per collection, share bằng email
- **Docker** — 4-service compose (PostgreSQL, Redis, Backend, Frontend)
- **Structured Logging** — Serilog (Console + File rolling daily)

---

## Tech Stack

| Layer | Công nghệ |
|-------|-----------|
| Backend | ASP.NET Core 9.0, EF Core 9, JWT + Identity |
| Frontend | React 19.2, Vite 7.3, React Router 7.13 |
| Database | SQLite (dev) / PostgreSQL 17 (prod) |
| Cache | Redis 7 |
| Real-time | SignalR 10.0 |
| Thumbnails | SixLabors.ImageSharp 3.x |
| Logging | Serilog (Console + File) |
| Deploy | Docker Compose, Nginx |

---

## Quick Start

### Docker (khuyến nghị)

```bash
git clone https://github.com/Thang4869/Visual-Asset-Hub.git
cd Visual-Asset-Hub   # thư mục gốc chứa docker-compose.yml
docker-compose up --build -d
```

| Service | URL |
|---------|-----|
| Frontend | http://localhost:3000 |
| Backend API | http://localhost:5027 |
| Swagger | http://localhost:5027/swagger |

### Local Development

**Backend:**
```bash
cd VAH.Backend
dotnet restore
dotnet run
# → http://localhost:5027
```

**Frontend:**
```bash
cd VAH.Frontend
npm install
npm run dev
# → http://localhost:5173
```

---

## API Endpoints (current)

### Auth
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| POST | `/api/v1/auth/register` | Đăng ký → JWT |
| POST | `/api/v1/auth/login` | Đăng nhập → JWT |

### Assets (Commands/Queries + Bulk + Layout)
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/v1/assets` | List (paged, sorted) |
| GET | `/api/v1/assets/{id}` | Chi tiết asset |
| POST | `/api/v1/assets` | Tạo asset mới |
| POST | `/api/v1/assets/upload` | Upload multi-file |
| PATCH | `/api/v1/assets/{id}` | Cập nhật một phần |
| DELETE | `/api/v1/assets/{id}` | Xóa asset |
| POST | `/api/v1/assets/{id}/duplicate` | Duplicate asset |
| PUT | `/api/v1/assets/{id}/position` | Cập nhật vị trí canvas |
| POST | `/api/v1/assets/reorder` | Sắp xếp lại thứ tự |
| POST | `/api/v1/assets/bulk-delete` | Xóa hàng loạt |
| POST | `/api/v1/assets/bulk-move` | Di chuyển hàng loạt |
| POST | `/api/v1/assets/bulk-move-group` | Di chuyển màu giữa nhóm |
| POST | `/api/v1/assets/bulk-tag` | Gắn/gỡ tag hàng loạt |

### Collections
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/v1/collections` | List (own + system + shared) |
| GET | `/api/v1/collections/{id}/items` | Items + sub-collections |
| POST | `/api/v1/collections` | Tạo mới |
| PATCH | `/api/v1/collections/{id}` | Cập nhật |
| DELETE | `/api/v1/collections/{id}` | Xóa |

### Tags
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/v1/tags` | List tags |
| GET | `/api/v1/tags/{id}` | Chi tiết tag |
| POST | `/api/v1/tags` | Tạo tag |
| PUT | `/api/v1/tags/{id}` | Cập nhật tag |
| DELETE | `/api/v1/tags/{id}` | Xóa tag |
| GET | `/api/v1/tags/asset/{assetId}` | Tags của asset |
| PUT | `/api/v1/tags/asset/{assetId}` | Set tags |
| POST | `/api/v1/tags/asset/{assetId}/add` | Thêm tags |
| POST | `/api/v1/tags/asset/{assetId}/remove` | Gỡ tags |
| POST | `/api/v1/tags/migrate` | Migrate legacy |

### Smart Collections
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/v1/smartcollections` | Danh sách smart collections |
| GET | `/api/v1/smartcollections/{id}/items` | Items phân trang |

### Search
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/v1/search` | Tìm kiếm assets + collections |

### Permissions
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/v1/collections/{collectionId}/permissions` | List permissions |
| POST | `/api/v1/collections/{collectionId}/permissions` | Cấp quyền |
| PUT | `/api/v1/collections/{collectionId}/permissions/{permissionId}` | Cập nhật role |
| DELETE | `/api/v1/collections/{collectionId}/permissions/{permissionId}` | Thu hồi |
| GET | `/api/v1/collections/{collectionId}/permissions/my-role` | Role hiện tại |
| GET | `/api/v1/shared-collections` | Collections được chia sẻ |

### Health
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/v1/health` | DB + Storage checks, env info |

---

## Cấu trúc dự án

```
1A/
├── docker-compose.yml
├── VAH.sln
├── README.md
├── docs/                       # 6 tài liệu
├── VAH.Backend/                # .NET 9 API
│   ├── Controllers/            # 9 controllers (incl. abstract base)
│   ├── Services/               # 24 service files (interfaces + implementations)
│   ├── Models/                 # 11 entities + DTOs + enums
│   ├── Data/                   # EF Core DbContext
│   ├── Hubs/                   # SignalR
│   ├── Middleware/             # Exception handling
│   └── Migrations/             # 5 migrations
└── VAH.Frontend/               # React 19 SPA
    └── src/
        ├── api/                # 11 API files (class-based)
        ├── hooks/              # 11 custom hooks
        └── components/         # 14 components
```

---

## Tài liệu

| File | Nội dung |
|------|----------|
| [docs/ARCHITECTURE_REVIEW.md](docs/ARCHITECTURE_REVIEW.md) | Kiến trúc hệ thống, tech stack, roadmap |
| [docs/PROJECT_DOCUMENTATION.md](docs/PROJECT_DOCUMENTATION.md) | Tài liệu kỹ thuật chi tiết (nguồn sự thật hiện tại) |
| [docs/IMPLEMENTATION_GUIDE.md](docs/IMPLEMENTATION_GUIDE.md) | Hướng dẫn cài đặt, vận hành, xử lý sự cố |
| [docs/CHANGELOG.md](docs/CHANGELOG.md) | Lịch sử thay đổi tóm tắt gần nhất |
| [docs/FIX_REPORT_20260227.md](docs/FIX_REPORT_20260227.md) | Lịch sử chi tiết theo session (legacy log được giữ lại) |
| [docs/OOP_ASSESSMENT.md](docs/OOP_ASSESSMENT.md) | Đánh giá OOP, design patterns, tiến trình refactor |
| [docs/PHASE1_ARCHIVE.md](docs/PHASE1_ARCHIVE.md) | Lưu trữ lịch sử Phase 1 |
| [docs/GLOSSARY.md](docs/GLOSSARY.md) | Bảng thuật ngữ chuẩn dùng chung cho toàn bộ tài liệu |

---

## License

MIT
