# DEPENDENCY GRAPH — Service Registration & Wiring

> **Last Updated**: 2026-03-08

---

## §1 — Dependency Injection Registration Groups

All services registered in `ServiceCollectionExtensions.cs` (6 extension methods):

```
Program.cs
    │
    ├── builder.Services.AddCorsPolicy(config)
    ├── builder.Services.AddRateLimitingPolicies()
    ├── builder.Services.AddDatabase(config)
    ├── builder.Services.AddIdentityAndAuth(config)
    ├── builder.Services.AddCachingServices(config)
    └── builder.Services.AddApplicationServices(config)
```

## §2 — Service Lifetime Matrix

### 2.1 Singleton Services

| Registration | Purpose |
|-------------|---------|
| `DatabaseProviderInfo` | Tracks which DB provider active (SQLite/PostgreSQL) |
| `FileUploadConfig` | Upload size/extension constraints |

### 2.2 Scoped Services (per-request)

| Interface | Implementation | Layer |
|-----------|---------------|-------|
| `IAssetApplicationService` | `AssetApplicationService` | Features/Application |
| `IUserContextProvider` | `UserContextProvider` | Features/Infrastructure |
| `IFileMapperService` | `FileMapperService` | Features/Infrastructure |
| `IAssetDuplicateStrategy` | `InPlaceDuplicateStrategy` | Features/Application/Duplicate |
| `IAssetDuplicateStrategy` | `TargetFolderDuplicateStrategy` | Features/Application/Duplicate |
| `IAssetDuplicateStrategyFactory` | `AssetDuplicateStrategyFactory` | Features/Application/Duplicate |
| `IStorageService` | `LocalStorageService` | Services |
| `IAssetService` | `AssetService` | Services |
| `IBulkAssetService` | `BulkAssetService` | Services |
| `ICollectionService` | `CollectionService` | Services |
| `IAuthService` | `AuthService` | Services |
| `IThumbnailService` | `ThumbnailService` | Services |
| `ITagService` | `TagService` | Services |
| `INotificationService` | `NotificationService` | Services |
| `ISmartCollectionService` | `SmartCollectionService` | Services |
| `ISearchService` | `SearchService` | Services |
| `IPermissionService` | `PermissionService` | Services |
| `IHealthCheckService` | `HealthCheckService` | Services |
| `AssetCleanupHelper` | (concrete) | Services |

### 2.3 Framework Registrations

| Registration | Lifetime | Purpose |
|-------------|----------|---------|
| `IHttpContextAccessor` | Singleton | Access HTTP context in services |
| `AppDbContext` | Scoped | EF Core database context |
| `MediatR` | Varies | CQRS command/query pipeline |
| `IExceptionHandler` | Scoped | RFC 7807 error formatting |
| `IDistributedCache` | Varies | Redis or in-memory fallback |
| `UserManager<ApplicationUser>` | Scoped | ASP.NET Identity |
| `SignInManager<ApplicationUser>` | Scoped | ASP.NET Identity |

## §3 — Service Dependency Graph

```
Controller Layer
├── AssetsCommandController       ──→ ISender (MediatR)
├── AssetsQueryController         ──→ ISender (MediatR)
├── BulkAssetsController          ──→ IBulkAssetService
├── CollectionsController         ──→ ICollectionService
├── TagsController                ──→ ITagService
├── SearchController              ──→ ISearchService
├── AuthController                ──→ IAuthService
├── PermissionsController         ──→ IPermissionService, ILogger
├── SharedCollectionsController    ──→ IPermissionService, ILogger
├── SmartCollectionsController    ──→ ISmartCollectionService
├── HealthController              ──→ IHealthCheckService
└── [Asset subtypes Controllers]  ──→ IAssetService, ILogger

CQRS Layer (MediatR Handlers)
├── UploadAssetsHandler         ──→ IAssetService
├── UpdateAssetHandler          ──→ IAssetService
├── DeleteAssetHandler          ──→ IAssetService
├── DuplicateAssetHandler       ──→ IAssetDuplicateStrategyFactory, AppDbContext
├── UpdateAssetPositionHandler  ──→ IAssetService
├── GetAssetsQuery              ──→ IAssetService
├── GetAssetByIdQuery           ──→ IAssetService
└── GetAssetsByFolderQuery      ──→ IAssetService

Application Layer (Facade)
└── AssetApplicationService     ──→ ISender, IUserContextProvider, IOptions<AssetOptions>

Service Layer
├── AssetService        ──→ AppDbContext, IStorageService, IThumbnailService,
│                    INotificationService, IWebHostEnvironment, ILogger
├── BulkAssetService    ──→ AppDbContext, INotificationService, ILogger,
│                        IStorageService, IWebHostEnvironment
├── CollectionService   ──→ AppDbContext, ILogger
├── TagService          ──→ AppDbContext, ILogger
├── SearchService       ──→ AppDbContext
├── SmartCollectionService  ──→ AppDbContext, IEnumerable<ISmartCollectionFilter>
├── PermissionService       ──→ AppDbContext, UserManager<ApplicationUser>, ILogger
├── AuthService             ──→ UserManager<ApplicationUser>, SignInManager<ApplicationUser>,
│                   IConfiguration, ICollectionService
├── NotificationService   ──→ IHubContext<AssetHub>
├── ThumbnailService      ──→ IWebHostEnvironment, ILogger
├── LocalStorageService   ──→ IWebHostEnvironment, ILogger
├── AssetCleanupHelper    ──→ IWebHostEnvironment
└── FileMapperService     ──→ (no dependencies)

Infrastructure Layer
├── AppDbContext            ──→ DatabaseProviderInfo
└── GlobalExceptionHandler  ──→ ILogger
```

## §4 — Cross-Cutting Concerns

### 4.1 Middleware Pipeline Order

```
Request →
  ├── ExceptionHandling (catch-all)
  ├── Serilog Request Logging
  ├── CORS ("Frontend" policy)
  ├── Rate Limiting
  ├── Authentication (JWT Bearer)
  ├── Authorization
  ├── Static Files (wwwroot/)
  ├── Routing
  │   ├── MapControllers
  │   └── MapHub<AssetHub>("/hubs/assets")
  └── Response
```

### 4.2 Rate Limiting Policies

| Policy | Window | Permit | Queue | Applied To |
|--------|--------|--------|-------|-----------|
| `Fixed` | 1 min | 100 | 10 | General API endpoints, `AuthController`, `TagsController` (migrate) |
| `Upload` | 1 min | 20 | 5 | File upload endpoints |
| `Search` | 1 min (sliding, 6 segments) | 60 | 0 | `SearchController` |

### 4.3 CORS Configuration

- Policy: `"Frontend"`
- Origins: Configurable via `Cors:AllowedOrigins` (default: `localhost:5173,5174`)
- Methods: Any
- Headers: Any
- Credentials: Allowed (required for SignalR)

## §5 — Configuration Binding

| Config Section | Bound To | Key Settings |
|---------------|----------|-------------|
| `Cors:AllowedOrigins` | `string[]` | Frontend URLs |
| `ConnectionStrings:DefaultConnection` | `AppDbContext` | DB connection |
| `ConnectionStrings:Redis` | `IDistributedCache` | Redis endpoint |
| `DatabaseProvider` | `DatabaseProviderInfo` | "SQLite" or "PostgreSQL" |
| `Jwt:SecretKey` | JWT Bearer | Signing key |
| `Jwt:Issuer` | JWT Bearer | Token issuer |
| `Jwt:Audience` | JWT Bearer | Token audience |
| `AssetOptions` | `IOptions<AssetOptions>` | Asset-specific settings |

---

## §6 — Blast Radius Analysis

> **Source**: Migrated from `PROJECT_DOCUMENTATION.md` §1.4

### Service Dependency Impact Matrix

| Service | Depends On | Depended By | Blast Radius if Changed |
|---------|-----------|-------------|------------------------|
| **AssetService** | DbContext, IStorageService, IThumbnailService, INotificationService, IPermissionService | Controllers, BulkAssetService | 🔴 High — core CRUD |
| **BulkAssetService** | DbContext, AssetCleanupHelper, IPermissionService, INotificationService | BulkAssetsController | 🟡 Medium |
| **CollectionService** | DbContext, IDistributedCache | Controllers | 🟡 Medium — cached |
| **SearchService** | DbContext | SearchController | 🟢 Low — isolated |
| **TagService** | DbContext | TagsController | 🟢 Low — isolated |
| **PermissionService** | DbContext, IDistributedCache | AssetService, BulkAssetService, Controllers | 🟠 High — authz dependency |
| **SmartCollectionService** | DbContext, ISmartCollectionFilter[] (5 strategies) | SmartCollectionsController | 🟢 Low — Strategy pattern isolates changes |
| **StorageService** | File system (wwwroot/) | AssetService, ThumbnailService | 🟠 High — all file I/O |
| **ThumbnailService** | IStorageService, ImageSharp | AssetService | 🟢 Low — post-processing only |
| **NotificationService** | SignalR IHubContext | AssetService, BulkAssetService, CollectionService | 🟢 Low — fire-and-forget |
| **AuthService** | UserManager, SignInManager, JWT config | AuthController | 🟢 Low — isolated |
| **AssetCleanupHelper** | IStorageService | AssetService, BulkAssetService | 🟢 Low — utility |

### Key Observations

1. **AppDbContext is the single dependency bottleneck** — all 12 services depend on it. This is the main coupling risk.
2. **PermissionService is a hidden critical path** — AssetService and BulkAssetService both depend on it for authz checks. A bug here = auth bypass.
3. **No circular dependencies** — the graph is a clean DAG (Directed Acyclic Graph).
4. **IStorageService is the swap point** — only AssetService and ThumbnailService touch it. Cloud migration blast radius is contained.

---

> **Document End**
