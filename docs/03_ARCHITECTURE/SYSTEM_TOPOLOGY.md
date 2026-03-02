# SYSTEM TOPOLOGY — Infrastructure & Deployment

> **Last Updated**: 2026-03-02

---

## §1 — Deployment Architecture

```
┌──────────────────────────────────────────────────────────┐
│                    Docker Compose Host                     │
│                                                            │
│  ┌──────────────┐  ┌──────────────┐  ┌───────────────┐  │
│  │   Frontend    │  │   Backend    │  │  PostgreSQL   │  │
│  │  React 19     │  │  .NET 9      │  │  17           │  │
│  │  Nginx:3000   │──│  Kestrel:5027│──│  :5432        │  │
│  │               │  │              │  │               │  │
│  │  Static SPA   │  │  REST API    │  │  6 tables     │  │
│  │  Reverse proxy│  │  SignalR Hub │  │  Identity     │  │
│  └──────────────┘  └──────┬───────┘  └───────────────┘  │
│                           │                               │
│                    ┌──────┴───────┐                       │
│                    │    Redis     │                       │
│                    │    :6379     │                       │
│                    │  Cache layer │                       │
│                    └──────────────┘                       │
│                                                            │
│  Volume Mounts:                                           │
│  ├── ./uploads → /app/wwwroot/uploads (asset files)       │
│  ├── ./vah-data → /var/lib/postgresql/data (DB)           │
│  └── ./logs → /app/logs (Serilog)                         │
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

> **Document End**
