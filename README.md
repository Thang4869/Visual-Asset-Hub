# Visual Asset Hub (VAH)

**Digital Asset Management** — Ứng dụng web quản lý tài nguyên số (ảnh, link, màu sắc) với phân quyền, real-time sync, và giao diện Dark Navy hiện đại.

![.NET 9](https://img.shields.io/badge/.NET-9.0-purple)
![React 19](https://img.shields.io/badge/React-19.2-blue)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-blue)
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
| Database | SQLite (dev) / PostgreSQL 16 (prod) |
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
cd 1A
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

## API Endpoints (38 total)

### Auth (4)
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| POST | `/api/Auth/register` | Đăng ký |
| POST | `/api/Auth/login` | Đăng nhập → JWT |
| GET | `/api/Auth/profile` | Thông tin user |
| POST | `/api/Auth/change-password` | Đổi mật khẩu |

### Assets (10)
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/Assets` | List (paged, sorted) |
| GET | `/api/Assets/{id}` | Chi tiết |
| POST | `/api/Assets` | Upload/Create |
| PUT | `/api/Assets/{id}` | Cập nhật |
| DELETE | `/api/Assets/{id}` | Xóa |
| POST | `/api/Assets/reorder` | Sắp xếp lại |
| POST | `/api/Assets/bulk-delete` | Xóa hàng loạt |
| POST | `/api/Assets/bulk-move` | Di chuyển hàng loạt |
| PUT | `/api/Assets/{id}/position` | Vị trí canvas |
| POST | `/api/Assets/add-link` | Thêm link |

### Collections (5)
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/Collections` | List (tree) |
| GET | `/api/Collections/{id}` | Chi tiết |
| POST | `/api/Collections` | Tạo mới |
| PUT | `/api/Collections/{id}` | Cập nhật |
| DELETE | `/api/Collections/{id}` | Xóa |

### Tags (7)
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/Tags` | List tags |
| POST | `/api/Tags` | Tạo tag |
| DELETE | `/api/Tags/{id}` | Xóa tag |
| GET | `/api/Tags/asset/{assetId}` | Tags của asset |
| PUT | `/api/Tags/asset/{assetId}` | Set tags (replace) |
| POST | `/api/Tags/asset/{assetId}/add` | Thêm tags |
| POST | `/api/Tags/migrate` | Migrate legacy |

### Smart Collections (2)
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/SmartCollections` | Danh sách |
| GET | `/api/SmartCollections/{id}/assets` | Assets trong SC |

### Search (2)
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/Search` | Tìm kiếm assets |
| GET | `/api/Search/suggestions` | Gợi ý search |

### Permissions (5)
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/Permissions/collection/{id}` | List permissions |
| POST | `/api/Permissions/grant` | Cấp quyền |
| DELETE | `/api/Permissions/revoke` | Thu hồi |
| GET | `/api/Permissions/shared-with-me` | Collections được chia sẻ |
| GET | `/api/Permissions/my-role/{collectionId}` | Role hiện tại |

### Health (3)
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/Health` | Quick check |
| GET | `/api/Health/detailed` | DB + Redis status |
| GET | `/api/Health/version` | App version |

---

## Cấu trúc dự án

```
1A/
├── docker-compose.yml
├── VAH.sln
├── README.md
├── docs/                       # 4 tài liệu
├── VAH.Backend/                # .NET 9 API
│   ├── Controllers/            # 8 controllers
│   ├── Services/               # 9 services
│   ├── Models/                 # 6 entities + DTOs
│   ├── Data/                   # EF Core DbContext
│   ├── Hubs/                   # SignalR
│   ├── Middleware/             # Exception handling
│   └── Migrations/             # 4 migrations
└── VAH.Frontend/               # React 19 SPA
    └── src/
        ├── api/                # 7 API modules
        ├── hooks/              # 6 custom hooks
        └── components/         # 12 components
```

---

## Tài liệu

| File | Nội dung |
|------|----------|
| [docs/ARCHITECTURE_REVIEW.md](docs/ARCHITECTURE_REVIEW.md) | Kiến trúc hệ thống, tech stack, roadmap |
| [docs/PROJECT_DOCUMENTATION.md](docs/PROJECT_DOCUMENTATION.md) | Tài liệu kỹ thuật chi tiết (models, services, APIs) |
| [docs/IMPLEMENTATION_GUIDE.md](docs/IMPLEMENTATION_GUIDE.md) | Hướng dẫn cài đặt, sử dụng, troubleshooting |
| [docs/FIX_REPORT_20260227.md](docs/FIX_REPORT_20260227.md) | Lịch sử phát triển & sửa lỗi |

---

## License

MIT
