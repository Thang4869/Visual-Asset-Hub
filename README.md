# Visual Asset Hub (VAH)

**Digital Asset Management** — Ứng dụng web quản lý tài nguyên số (ảnh, link, màu sắc) với phân quyền, real-time sync, và giao diện Dark Navy hiện đại.

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

## API Endpoints (43 total)

### Auth — 2 endpoints [RateLimited]
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| POST | `/api/Auth/register` | Đăng ký → JWT |
| POST | `/api/Auth/login` | Đăng nhập → JWT |

### Assets — 16 endpoints [Authorize]
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/Assets` | List (paged, sorted) |
| POST | `/api/Assets` | Tạo asset mới |
| POST | `/api/Assets/upload` | Upload multi-file (validation: size, ext, MIME) |
| PUT | `/api/Assets/{id}/position` | Vị trí canvas |
| POST | `/api/Assets/create-folder` | Tạo thư mục |
| POST | `/api/Assets/create-color` | Tạo asset màu sắc |
| POST | `/api/Assets/create-color-group` | Tạo nhóm màu |
| POST | `/api/Assets/create-link` | Tạo liên kết (URL validation) |
| PUT | `/api/Assets/{id}` | Cập nhật asset |
| DELETE | `/api/Assets/{id}` | Xóa asset + file + thumbnails |
| POST | `/api/Assets/reorder` | Sắp xếp lại thứ tự |
| GET | `/api/Assets/group/{groupId}` | Assets theo nhóm |
| POST | `/api/Assets/bulk-delete` | Xóa hàng loạt |
| POST | `/api/Assets/bulk-move` | Di chuyển hàng loạt |
| POST | `/api/Assets/bulk-move-group` | Di chuyển màu giữa các group |
| POST | `/api/Assets/bulk-tag` | Gắn/gỡ tag hàng loạt |

### Collections — 5 endpoints [Authorize]
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/Collections` | List (own + system + shared) |
| GET | `/api/Collections/{id}/items` | Items + sub-collections |
| POST | `/api/Collections` | Tạo mới |
| PUT | `/api/Collections/{id}` | Cập nhật |
| DELETE | `/api/Collections/{id}` | Xóa (owner only) |

### Tags — 10 endpoints [Authorize]
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/Tags` | List tags |
| GET | `/api/Tags/{id}` | Chi tiết tag |
| POST | `/api/Tags` | Tạo tag (dedup normalized) |
| PUT | `/api/Tags/{id}` | Cập nhật tag |
| DELETE | `/api/Tags/{id}` | Xóa tag |
| GET | `/api/Tags/asset/{assetId}` | Tags của asset |
| PUT | `/api/Tags/asset/{assetId}` | Set tags (replace) |
| POST | `/api/Tags/asset/{assetId}/add` | Thêm tags |
| POST | `/api/Tags/asset/{assetId}/remove` | Gỡ tags |
| POST | `/api/Tags/migrate` | Migrate legacy → M2M |

### Smart Collections — 2 endpoints [Authorize]
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/SmartCollections` | Danh sách (8 built-in + per-tag) |
| GET | `/api/SmartCollections/{id}/items` | Items phân trang |

### Search — 1 endpoint [Authorize]
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/Search` | Tìm kiếm assets + collections |

### Permissions — 6 endpoints [Authorize]
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `.../permissions` | List permissions |
| POST | `.../permissions` | Cấp quyền (owner only, by email) |
| PUT | `.../permissions/{permissionId}` | Cập nhật role |
| DELETE | `.../permissions/{permissionId}` | Thu hồi |
| GET | `.../permissions/my-role` | Role hiện tại |
| GET | `/api/shared-collections` | Collections được chia sẻ |

### Health — 1 endpoint [No Auth]
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/Health` | DB + Storage checks, env info |

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
| [docs/PROJECT_DOCUMENTATION.md](docs/PROJECT_DOCUMENTATION.md) | Tài liệu kỹ thuật chi tiết (models, services, APIs) |
| [docs/IMPLEMENTATION_GUIDE.md](docs/IMPLEMENTATION_GUIDE.md) | Hướng dẫn cài đặt, sử dụng, troubleshooting |
| [docs/FIX_REPORT_20260227.md](docs/FIX_REPORT_20260227.md) | Lịch sử phát triển & sửa lỗi |
| [docs/OOP_ASSESSMENT.md](docs/OOP_ASSESSMENT.md) | Đánh giá OOP, design patterns, tiến trình refactor |
| [docs/PHASE1_REPORT.md](docs/PHASE1_REPORT.md) | Báo cáo Phase 1 (historical snapshot) |

---

## License

MIT
