# ADR-004: Dual Database Provider (SQLite + PostgreSQL)

> **Status**: Accepted
> **Date**: 2026-02-25
> **Deciders**: Tech Lead

## Context

Development requires a frictionless local setup (no Docker dependency for quick iterations), while production requires a robust, concurrent-safe database. Running PostgreSQL locally via Docker is viable but slows the edit-compile-run cycle and creates setup barriers for new contributors.

## Decision

Support **dual database providers** configurable via `appsettings.json`:

```json
// Development (appsettings.Development.json)
{
  "DatabaseProvider": "SQLite",
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=vah.db"
  }
}

// Production (appsettings.json / Docker env)
{
  "DatabaseProvider": "PostgreSQL",
  "ConnectionStrings": {
    "DefaultConnection": "Host=postgres;Database=vah;Username=vah;Password=..."
  }
}
```

Implementation in `ServiceCollectionExtensions.AddDatabase()`:

```csharp
if (dbProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
    options.UseNpgsql(connectionString);
else
    options.UseSqlite(connectionString);
```

A `DatabaseProviderInfo` singleton is registered so `AppDbContext` can adapt SQL dialect for raw queries (e.g., discriminator fix in `Program.cs`).

## Consequences

### Positive
- Zero-dependency local development: `dotnet run` with SQLite — no Docker needed
- Production-grade PostgreSQL 17 with proper concurrency, indexing, full-text search
- Same EF Core migrations work for both providers (LINQ-based)

### Negative
- Raw SQL must be provider-aware (already handled via `DatabaseProviderInfo`)
- Feature drift risk: PostgreSQL-specific features (e.g., `jsonb`, full-text) can't be used in shared migrations
- SQLite limitations: no concurrent writes, weaker type system, no `ALTER COLUMN`

### Neutral
- Auto-migration on startup (`context.Database.Migrate()`) works for both providers
- Docker Compose always uses PostgreSQL — SQLite is only for `dotnet run` without Docker

## Compliance

- All EF queries must use LINQ — raw SQL only via `AppDbContext` methods that check `DatabaseProviderInfo`
- Integration tests should run against both providers
- New migrations must be tested with `dotnet ef database update` on both SQLite and PostgreSQL

---

> **Document End**
