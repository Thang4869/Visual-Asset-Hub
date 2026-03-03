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

## §8 — Deployment Rollback Strategy

> **Source**: Migrated from `IMPLEMENTATION_GUIDE.md` §9

### Database Migration Rollback

EF Core migrations CAN be rolled back by specifying a previous migration:

```bash
# List applied migrations
dotnet ef migrations list

# Rollback to specific migration
dotnet ef database update <PreviousMigrationName>

# Rollback ALL migrations (removes entire schema — DANGEROUS)
dotnet ef database update 0
```

**Important:**
- **Always backup database before applying new migrations** (see §9)
- Migrations with destructive operations (DROP COLUMN, DROP TABLE) **cannot rollback data** — only schema
- Recommendation: write rollback SQL scripts for each migration with destructive ops

### Application Rollback (Docker)

```bash
# List built image versions
docker images | grep vah

# Rollback by running previous image
docker-compose down
# Edit docker-compose.yml to old image tag, or:
docker-compose up -d --no-build    # use cached image

# Or via git tag:
git checkout v1.2.3
docker-compose up --build -d
```

### Emergency Rollback Checklist

1. ☐ Stop traffic (if load balancer: drain)
2. ☐ `docker-compose down` (stop all services)
3. ☐ Restore database from backup (see §9)
4. ☐ Rollback EF migration if needed: `dotnet ef database update <target>`
5. ☐ Deploy previous version: `git checkout <tag> && docker-compose up --build -d`
6. ☐ Verify: health endpoint, login, asset CRUD
7. ☐ Notify users if applicable

---

## §9 — Backup & Restore

> **Source**: Migrated from `IMPLEMENTATION_GUIDE.md` §10

### PostgreSQL Backup

```bash
# Manual backup
docker exec vah-postgres pg_dump -U vah_user vah_database > backup_$(date +%Y%m%d_%H%M%S).sql

# Compressed backup
docker exec vah-postgres pg_dump -U vah_user -Fc vah_database > backup.dump

# Upload volumes backup
tar czf uploads_backup_$(date +%Y%m%d).tar.gz VAH.Backend/wwwroot/uploads/
```

### PostgreSQL Restore

```bash
# Restore from SQL dump
docker exec -i vah-postgres psql -U vah_user vah_database < backup_20260301_120000.sql

# Restore from compressed dump
docker exec -i vah-postgres pg_restore -U vah_user -d vah_database --clean < backup.dump

# Restore uploads
tar xzf uploads_backup_20260301.tar.gz -C .
```

### Automated Backup (recommended — NOT YET IMPLEMENTED)

Add cron job or systemd timer:
```bash
# /etc/cron.d/vah-backup (runs daily at 3:00 AM)
0 3 * * * root docker exec vah-postgres pg_dump -U vah_user -Fc vah_database > /backups/vah_$(date +\%Y\%m\%d).dump && find /backups -mtime +30 -delete
```

> ⚠️ **NOT YET IMPLEMENTED.** This is a critical operational gap identified in [INCIDENT_RESPONSE.md](INCIDENT_RESPONSE.md) (Failure Mode FM5) and ARCHITECTURE_REVIEW.md §26 (Assumption Critique A3). Prioritize setup before production deployment.

---

> **Document End**
