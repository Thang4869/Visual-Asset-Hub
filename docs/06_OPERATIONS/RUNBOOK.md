# RUNBOOK — Development & Deployment Operations

> **Last Updated**: 2026-03-02

---

## §1 — Local Development Setup

### Prerequisites

| Tool | Version | Purpose |
|------|---------|---------|
| .NET SDK | 9.0+ | Backend build/run |
| Node.js | 20+ | Frontend build/run |
| Docker | 24+ | Full-stack containers |
| Git | 2.40+ | Source control |

### Backend Only (SQLite)

```bash
cd VAH.Backend
dotnet restore
dotnet run
# → http://localhost:5027
# → Swagger: http://localhost:5027/swagger
```

### Frontend Only

```bash
cd VAH.Frontend
npm install
npm run dev
# → http://localhost:5173
```

### Full Stack (Docker Compose)

```bash
docker compose up -d
# Frontend:   http://localhost:3000
# Backend:    http://localhost:5027
# PostgreSQL: localhost:5432
# Redis:      localhost:6379
```

## §2 — Database Operations

### Run Migrations

```bash
cd VAH.Backend

# Development (SQLite)
dotnet ef database update

# Add new migration
dotnet ef migrations add <MigrationName>

# Rollback to specific migration
dotnet ef database update <PreviousMigrationName>
```

### Auto-Migration (Startup)

Production/Docker always runs `context.Database.Migrate()` on startup. After migration, a discriminator fix SQL runs to correct any rows with incorrect `ContentType` values.

### Reset Database

```bash
# SQLite — delete the file
rm vah.db

# PostgreSQL — drop and recreate
docker compose down -v    # Removes volumes
docker compose up -d      # Fresh start
```

## §3 — Docker Operations

### Build Images

```bash
# Build all
docker compose build

# Build specific service
docker compose build backend
docker compose build frontend
```

### Logs

```bash
# All services
docker compose logs -f

# Specific service
docker compose logs -f backend

# Last 100 lines
docker compose logs --tail=100 backend
```

### Restart Services

```bash
docker compose restart backend
docker compose restart frontend
```

## §4 — Health Checks

### Backend Health

```bash
curl http://localhost:5027/api/v1/health
```

Response:
```json
{
  "status": "healthy",
  "database": "connected",
  "storage": "accessible",
  "timestamp": "2026-03-02T10:00:00Z"
}
```

### Service Status

```bash
docker compose ps
```

## §5 — Configuration Reference

### Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `DatabaseProvider` | `SQLite` | `SQLite` or `PostgreSQL` |
| `ConnectionStrings__DefaultConnection` | (varies) | DB connection string |
| `ConnectionStrings__Redis` | (empty) | Redis connection (optional) |
| `Jwt__SecretKey` | (required) | JWT signing key (≥256-bit) |
| `Jwt__Issuer` | `VAH` | Token issuer |
| `Jwt__Audience` | `VAH` | Token audience |
| `Cors__AllowedOrigins__0` | `localhost:5173` | Frontend URL |

### Configuration Files

| File | Environment | Key Difference |
|------|-------------|---------------|
| `appsettings.json` | All | Base config, PostgreSQL defaults |
| `appsettings.Development.json` | Development | SQLite, debug logging |

## §6 — Log Analysis

Logs written by Serilog to `VAH.Backend/logs/` directory:

```bash
# Recent logs
cat VAH.Backend/logs/log-20260302.txt | tail -50

# Search for errors
grep -i "error\|exception" VAH.Backend/logs/log-*.txt

# Request timing
grep "Request finished" VAH.Backend/logs/log-*.txt
```

### Log Levels

| Level | When Used |
|-------|-----------|
| Debug | Development only — detailed EF queries, handler entry/exit |
| Information | Request start/finish, service registrations, migrations |
| Warning | Rate limit hits, deprecated feature usage |
| Error | Unhandled exceptions, DB connection failures |
| Fatal | Application startup failures |

---

> **Document End**
