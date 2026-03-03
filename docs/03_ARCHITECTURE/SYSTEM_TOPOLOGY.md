# SYSTEM TOPOLOGY — Infrastructure & Deployment

> **Last Updated**: 2026-03-02

---

## §1 — Deployment Architecture

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

## §2 — Service Details

| Service | Image | Port | Health Check | Dependencies |
|---------|-------|------|-------------|-------------|
| **Frontend** | `node:20` → Nginx | 3000 | HTTP GET / | None |
| **Backend** | `mcr.microsoft.com/dotnet/aspnet:9.0` | 5027 | GET `/api/v1/health` | PostgreSQL, Redis (optional) |
| **PostgreSQL** | `postgres:17` | 5432 | `pg_isready` | None |
| **Redis** | `redis:7` | 6379 | `redis-cli ping` | None |

## §3 — Network Flow

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

## §4 — Environment Matrix

| Setting | Development | Staging | Production |
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

## §5 — Local Development Setup

```bash
# Backend (SQLite mode)
cd VAH.Backend
dotnet run                    # → http://localhost:5027

# Frontend
cd VAH.Frontend
npm install && npm run dev    # → http://localhost:5173

# Docker Compose (full stack)
docker compose up -d          # Frontend:3000, Backend:5027, PG:5432, Redis:6379
```

## §6 — File Storage Layout

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

## §7 — Target Deployment Architecture

> **Source**: Migrated from `ARCHITECTURE_REVIEW.md` §14

### Current: Single Instance

```
docker-compose.yml
├── postgres:17-alpine     (port 5432, healthcheck: pg_isready)
├── redis:7-alpine         (port 6379, healthcheck: redis-cli ping)
├── backend (multi-stage)  (port 5027, healthcheck: /api/v1/Health)
└── frontend (Nginx)       (port 80, SPA fallback)

Volumes: postgres-data, redis-data, backend-uploads, backend-logs
```

### Target: Scalable Production

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

**Prerequisites for target architecture:**
1. Cloud storage implementation (IStorageService already abstracted)
2. SignalR Redis backplane configuration
3. Externalized secrets (Key Vault / SSM)
4. Managed PostgreSQL (RDS / Cloud SQL)
5. Health check readiness endpoint (separate from liveness)

---

## §8 — Environment Strategy

> **Source**: Migrated from `ARCHITECTURE_REVIEW.md` §15

| Aspect | Development | Staging | Production |
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

### Environment Parity Principle

Staging must mirror production infrastructure to catch environment-specific bugs (especially SQLite↔PostgreSQL drift). Dev may diverge for convenience but must run the full integration test suite against PostgreSQL before merge.

### Current Gap

**No staging environment exists.** Code goes from dev → production. This is the primary operational risk. Adding a staging environment (even as a second docker-compose profile) is a prerequisite for safe deployments.

---

## §9 — Data Flow Diagrams

> **Source**: Migrated from `PROJECT_DOCUMENTATION.md` §1.1–1.3

### 9.1 Upload Flow

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

### 9.2 Read Flow (GET Assets)

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

### 9.3 Cache Invalidation Flow

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
