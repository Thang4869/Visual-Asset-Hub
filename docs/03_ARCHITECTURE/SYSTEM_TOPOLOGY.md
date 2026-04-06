# TOPOLOGY HỆ THỐNG — Hạ tầng & Triển khai

> **Last Updated**: 2026-04-06

---

## §1 — Kiến trúc triển khai

```
┌──────────────────────────────────────────────────────────┐
│                    Docker Compose Host                   │
│                                                          │
│  ┌───────────────┐  ┌──────────────┐  ┌───────────────┐  │
│  │   Frontend    │  │   Backend    │  │  PostgreSQL   │  │
│  │  React 19     │  │  .NET 9      │  │  17           │  │
│  │  Nginx:3000   │──│  Kestrel:5027│──│  :5432        │  │
│  │               │  │              │  │               │  │
│  │  Static SPA   │  │  REST API    │  │  6 tables     │  │
│  │  Reverse proxy│  │  SignalR Hub │  │  Identity     │  │
│  └───────────────┘  └──────┬───────┘  └───────────────┘  │
│                            │                             │
│                     ┌──────┴───────┐                     │
│                     │    Redis     │                     │
│                     │    :6379     │                     │
│                     │  Cache layer │                     │
│                     └──────────────┘                     │
│                                                          │
│  Volume Mounts:                                          │
│  ├── ./uploads → /app/wwwroot/uploads (asset files)      │
│  ├── ./vah-data → /var/lib/postgresql/data (DB)          │
│  └── ./logs → /app/logs (Serilog)                        │
└──────────────────────────────────────────────────────────┘
```

## §2 — Chi tiết dịch vụ

| Dịch vụ | Image | Port | Health Check | Phụ thuộc |
|---------|-------|------|-------------|-------------|
| **Frontend** | `node:20` → Nginx | 3000 | HTTP GET / | None |
| **Backend** | `mcr.microsoft.com/dotnet/aspnet:9.0` | 5027 | GET `/api/v1/health` | PostgreSQL, Redis (optional) |
| **PostgreSQL** | `postgres:17` | 5432 | `pg_isready` | None |
| **Redis** | `redis:7` | 6379 | `redis-cli ping` | None |

## §3 — Luồng mạng

```
Browser (SPA)
    │
    ├── Static assets ──→ Nginx (port 3000) ──→ /dist/index.html, /assets/*
    │
    ├── API calls ──────→ Nginx reverse proxy ──→ Backend (port 5027)
    │   └── /api/v1/*         /api/v1/*
    │
    └── SignalR ────────→ Nginx WebSocket ──→ Backend /hubs/assets
        └── wss://           upgrade
```

## §4 — Ma trận môi trường

| Cấu hình | Development | Staging | Production |
|---------|------------|---------|------------|
| DB Provider | SQLite | PostgreSQL | PostgreSQL |
| Redis | None (in-memory) | Redis | Redis |
| Migrations | Auto on startup | Auto | Manual CLI |
| Swagger | Enabled | Enabled | Disabled |
| Log Level | Debug | Information | Warning |
| Error Detail | Full stack trace | Message only | Generic message |
| CORS Origins | `localhost:5173,5174` | Staging URL | Production URL |
| Kestrel Body Limit | 100 MB | 100 MB | 50 MB |
| Rate Limit (Fixed) | 100/min | 100/min | 60/min |

## §5 — Thiết lập phát triển cục bộ

```bash
# Backend (SQLite mode)
cd VAH.Backend
dotnet run                    # → http://localhost:5027

# Frontend
cd src/VAH.Frontend
npm install && npm run dev    # → http://localhost:5173

# Docker Compose (full stack)
docker compose up -d          # Frontend:3000, Backend:5027, PG:5432, Redis:6379
```

## §6 — Bố cục lưu trữ tệp

```
wwwroot/
└── uploads/
    ├── {uuid}.{ext}              # Original uploaded files
    └── thumbs/
        ├── sm_{uuid}.webp        # 150px thumbnail
        ├── md_{uuid}.webp        # 400px thumbnail
        └── lg_{uuid}.webp        # 800px thumbnail
```

---

## §7 — Kiến trúc triển khai mục tiêu

> **Nguồn**: Di chuyển từ `ARCHITECTURE_REVIEW.md` §14

### Hiện tại: Single Instance

```
docker-compose.yml
├── postgres:17-alpine     (port 5432, healthcheck: pg_isready)
├── redis:7-alpine         (port 6379, healthcheck: redis-cli ping)
├── backend (multi-stage)  (port 5027, healthcheck: /api/v1/Health)
└── frontend (Nginx)       (port 80, SPA fallback)

Volumes: postgres-data, redis-data, backend-uploads, backend-logs
```

### Mục tiêu: Production có khả năng mở rộng

```
                    Load Balancer (TLS termination)
                           │
              ┌────────────┼────────────┐
              ▼            ▼            ▼
         Backend ×N   Backend ×N   Backend ×N
              │            │            │
              └────────────┴────────────┘
                    │           │
              PostgreSQL    Redis Cluster
              (managed)     (backplane + cache)
                                │
                        Object Storage (S3)
                                │
                            CDN Edge
```

**Điều kiện tiên quyết cho kiến trúc mục tiêu:**
1. Cloud storage implementation (IStorageService already abstracted)
2. SignalR Redis backplane configuration
3. Externalized secrets (Key Vault / SSM)
4. Managed PostgreSQL (RDS / Cloud SQL)
5. Health check readiness endpoint (separate from liveness)

---

## §8 — Chiến lược môi trường

> **Nguồn**: Di chuyển từ `ARCHITECTURE_REVIEW.md` §15

| Khía cạnh | Development | Staging | Production |
|--------|-------------|---------|------------|
| **Database** | SQLite (zero-config) | PostgreSQL (Docker) | PostgreSQL (managed RDS/Cloud SQL) |
| **Cache** | In-memory (no Redis) | Redis (Docker) | Redis (managed ElastiCache/Memorystore) |
| **Storage** | Local `wwwroot/uploads` | Local or S3 | S3 / Azure Blob |
| **Auth** | JWT (relaxed, long TTL for testing) | JWT (prod-like) | JWT (strict, HTTPS only) |
| **Migrations** | Auto-migrate on startup | Auto-migrate (gated) | **Explicit CLI step before deploy** |
| **Swagger** | Enabled | Enabled (read-only) | Disabled or behind auth |
| **HTTPS** | HTTP (localhost) | HTTPS (self-signed OK) | HTTPS (valid cert, HSTS) |
| **Secrets** | `appsettings.Development.json` | Environment variables | Docker secrets / Key Vault |
| **Logging** | Console + File (verbose) | Console + File (info) | Structured (JSON) + centralized sink |
| **Error Detail** | Full stack traces | Sanitized | ProblemDetails only (no internals) |
| **SignalR** | Single instance | Single instance | Redis backplane (if multi-instance) |
| **Monitoring** | None | Health endpoint | Health + Metrics + Alerting |

### Nguyên tắc tương đồng môi trường

Staging phải phản chiếu hạ tầng production để phát hiện lỗi theo môi trường (đặc biệt là sai lệch SQLite↔PostgreSQL). Development có thể khác biệt để thuận tiện, nhưng phải chạy toàn bộ integration test suite trên PostgreSQL trước khi merge.

### Khoảng trống hiện tại

**Chưa có staging environment.** Code đi thẳng từ dev → production. Đây là rủi ro vận hành chính. Việc thêm staging environment (dù chỉ là một docker-compose profile thứ hai) là điều kiện tiên quyết cho triển khai an toàn.

---

## §9 — Sơ đồ luồng dữ liệu

> **Nguồn**: Di chuyển từ `PROJECT_DOCUMENTATION.md` §1.1–1.3

### §9.1 — Upload Flow

```
User (Browser)
  │  POST /api/v1/assets/upload (multipart/form-data)
  ▼
Nginx (port 80)
  │  proxy_pass → backend:5027, max body 100MB
  ▼
ASP.NET Middleware Pipeline
  │  GlobalExceptionHandler → CORS → RateLimit (20/min upload)
  │  → Auth (JWT Bearer) → Controller
  ▼
AssetsCommandController.Upload()
  │  Extract UserId from JWT claims
  ▼
AssetService.CreateAssetFromUploadAsync()
  │
  ├──① Validate: size ≤50MB, extension whitelist, MIME check
  ├──② IStorageService.SaveFileAsync()                  → wwwroot/uploads/{guid}.{ext}
  ├──③ AssetFactory.CreateImage() / CreateFile()        → TPH subtype
  ├──④ AppDbContext.Assets.Add()                        → SaveChangesAsync()
  ├──⑤ IThumbnailService.GenerateThumbnailsAsync()      → sm/md/lg WebP
  ├──⑥ IDistributedCache.RemoveAsync("collections:*")   → Redis / in-memory
  └──⑦ INotificationService.NotifyAssetCreated()        → SignalR → all user clients
```

### §9.2 — Read Flow (GET Assets)

```
Browser → Nginx → Auth → AssetsQueryController.GetAssets()
  │
  ▼
AssetService.GetAssetsAsync(paginationParams, userId)
  │
  ├── AppDbContext.Assets
  │     .Where(UserId == userId, CollectionId == collectionId)
  │     .Include(AssetTags → Tag)
  │     .OrderBy(SortOrder).Skip/Take
  │
  └── Return PagedResult<Asset> → JSON → 200 OK
        → Frontend: axios → useAssets hook → AssetGrid render
```

### §9.3 — Cache Invalidation Flow

```
Write operation (Create/Update/Delete)
  │
  ├──① AppDbContext.SaveChangesAsync()
  ├──② IDistributedCache.RemoveAsync("collections:{userId}")
  └──③ SignalR Hub.SendAsync("AssetChanged", payload)
        → All connected clients → useSignalR → refetch → cache MISS → DB → cache SET
```

---

> **Document End**
