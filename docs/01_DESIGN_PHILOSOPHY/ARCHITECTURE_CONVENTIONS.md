# ARCHITECTURE CONVENTIONS — OOP Standards for .NET 9 & React 19

> **Document Type**: Prescriptive (MUST follow)  
> **Scope**: VAH Backend (.NET 9) & Frontend (React 19)  
> **Status**: Active  
> **Last Updated**: 2026-03-02  
> **Owner**: Software Architect / Technical Lead

---

## Table of Contents

- [§1 — Document Purpose & Scope](#1--document-purpose--scope)
- [§2 — Core OOP Pillars & Enforcement Rules](#2--core-oop-pillars--enforcement-rules)
- [§3 — SOLID Principles — Concrete Rules](#3--solid-principles--concrete-rules)
- [§4 — Clean Architecture Layers](#4--clean-architecture-layers)
- [§5 — Interface Design Standards](#5--interface-design-standards)
- [§6 — Abstract Class Standards](#6--abstract-class-standards)
- [§7 — Dependency Injection Rules](#7--dependency-injection-rules)
- [§8 — Design Patterns — When & Why](#8--design-patterns--when--why)
- [§9 — Entity & Domain Model Conventions](#9--entity--domain-model-conventions)
- [§10 — Service Layer Conventions](#10--service-layer-conventions)
- [§11 — CQRS & MediatR Conventions](#11--cqrs--mediatr-conventions)
- [§12 — Exception Handling Architecture](#12--exception-handling-architecture)
- [§13 — Frontend OOP Conventions (React 19)](#13--frontend-oop-conventions-react-19)
- [§14 — XML Documentation Standards (.NET 9)](#14--xml-documentation-standards-net-9)
- [§15 — JSDoc Standards (React 19)](#15--jsdoc-standards-react-19)
- [§16 — Anti-Patterns & Violations](#16--anti-patterns--violations)
- [§17 — Compliance Checklist](#17--compliance-checklist)
- [§18 — Appendix: Decision Matrix](#18--appendix-decision-matrix)

---

## §1 — Document Purpose & Scope

### 1.1 Mục đích

File này định nghĩa **tiêu chuẩn OOP bắt buộc** cho toàn bộ codebase VAH. Mọi Pull Request PHẢI tuân thủ các quy tắc trong document này. Reviewer có quyền reject PR nếu vi phạm bất kỳ rule nào có tag `[MUST]`.

### 1.2 Severity Levels

| Tag | Ý nghĩa | PR Action |
|-----|---------|-----------|
| `[MUST]` | Bắt buộc — không có ngoại lệ | Reject nếu vi phạm |
| `[SHOULD]` | Khuyến khích mạnh — cần justification nếu bỏ qua | Comment + yêu cầu giải thích |
| `[MAY]` | Tùy chọn — áp dụng khi hợp lý | Suggestion only |

### 1.3 Phạm vi áp dụng

```
VAH.Backend/          → §2-§12, §14, §16-§17
VAH.Frontend/         → §2 (adapted), §13, §15, §16-§17
Shared conventions    → §1, §16, §17
```

---

## §2 — Core OOP Pillars & Enforcement Rules

### 2.1 Encapsulation `[MUST]`

**Nguyên tắc**: Mỗi class chỉ expose những gì cần thiết. Internal state PHẢI được bảo vệ.

```csharp
// ✅ ĐÚNG — Domain entity với encapsulated state
public class Asset
{
    // Private setters — state chỉ thay đổi qua domain methods
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int PositionX { get; private set; }
    public int PositionY { get; private set; }
    
    // Navigation properties — không expose setter
    public virtual ICollection<AssetTag> AssetTags { get; private set; } = new List<AssetTag>();
    
    /// <summary>
    /// Domain method — duy nhất cách hợp lệ để thay đổi position.
    /// </summary>
    public void UpdatePosition(int x, int y)
    {
        // Validation logic ở đây, KHÔNG ở Service
        PositionX = x;
        PositionY = y;
    }
}

// ❌ SAI — Public setters cho phép bypass business rules
public class Asset
{
    public int PositionX { get; set; }  // Ai cũng có thể set trực tiếp
    public int PositionY { get; set; }  // Không có validation
}
```

**Rules:**
| ID | Rule | Severity |
|----|------|----------|
| ENC-01 | Entity properties PHẢI dùng `private set` hoặc `init` | `[MUST]` |
| ENC-02 | State mutation PHẢI qua domain methods | `[MUST]` |
| ENC-03 | Collection navigation properties PHẢI return `ICollection<T>` hoặc `IReadOnlyCollection<T>` | `[MUST]` |
| ENC-04 | Internal helper classes PHẢI đặt `internal` access modifier | `[SHOULD]` |

### 2.2 Abstraction `[MUST]`

**Nguyên tắc**: Depend on abstractions (interfaces), not concretions (implementations).

```csharp
// ✅ ĐÚNG — Controller depends on interface
public class AssetLayoutController : BaseApiController
{
    private readonly IAssetService _assetService;
    
    public AssetLayoutController(IAssetService assetService) 
        => _assetService = assetService;
}

// ❌ SAI — Controller depends on concrete class
public class AssetLayoutController : BaseApiController
{
    private readonly AssetService _assetService;  // Tight coupling!
}
```

**Rules:**
| ID | Rule | Severity |
|----|------|----------|
| ABS-01 | Mọi Service PHẢI có corresponding Interface | `[MUST]` |
| ABS-02 | Constructor injection PHẢI dùng Interface type | `[MUST]` |
| ABS-03 | Storage, Caching, Notification — PHẢI abstract qua interface | `[MUST]` |
| ABS-04 | Không được `new` Service trong Service khác | `[MUST]` |

### 2.3 Inheritance `[SHOULD]`

**Nguyên tắc**: Prefer composition over inheritance. Chỉ dùng inheritance khi có **"is-a"** relationship rõ ràng.

```csharp
// ✅ ĐÚNG — TPH inheritance cho Asset types (clear "is-a" relationship)
public abstract class Asset { /* base properties */ }
public class ImageAsset : Asset 
{ 
    public override bool HasPhysicalFile => true;
    public override bool CanHaveThumbnails => true;
}
public class LinkAsset : Asset 
{ 
    public override bool HasPhysicalFile => false;
}

// ✅ ĐÚNG — Base controller (shared behavior for all API controllers)
public abstract class BaseApiController : ControllerBase
{
    protected string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
}

// ❌ SAI — Inheritance chỉ để reuse code (không có "is-a")
public class TagService : AssetService { }  // Tag không phải Asset!
```

**Rules:**
| ID | Rule | Severity |
|----|------|----------|
| INH-01 | Inheritance chain tối đa 2 levels (Base → Concrete) | `[MUST]` |
| INH-02 | Base class PHẢI là `abstract` (không instantiate trực tiếp) | `[MUST]` |
| INH-03 | Override methods PHẢI gọi `base.Method()` nếu base có logic | `[SHOULD]` |
| INH-04 | Prefer composition (inject dependency) over inheritance để reuse code | `[SHOULD]` |

### 2.4 Polymorphism `[MUST]`

**Nguyên tắc**: Dùng `virtual`/`override` và interface polymorphism để thay đổi behavior mà không sửa calling code.

```csharp
// ✅ ĐÚNG — Polymorphism qua virtual properties (TPH)
Asset asset = AssetFactory.CreateImage(userId, "photo.jpg", ...);
if (asset.HasPhysicalFile)           // ImageAsset → true
    await _storageService.DeleteAsync(asset.FilePath);
if (asset.CanHaveThumbnails)         // ImageAsset → true
    await _thumbnailService.DeleteAsync(asset.Id);

// ✅ ĐÚNG — Interface polymorphism (Strategy Pattern)
public interface ISmartCollectionFilter
{
    string FilterType { get; }
    Task<SmartCollectionDefinitionDto> GetDefinitionAsync();
    IQueryable<Asset> ApplyFilter(IQueryable<Asset> query, ...);
}

// Runtime: đúng strategy được chọn dựa trên FilterType
ISmartCollectionFilter filter = _filters.First(f => f.FilterType == type);
var results = filter.ApplyFilter(baseQuery, parameters);
```

**Rules:**
| ID | Rule | Severity |
|----|------|----------|
| POL-01 | Behavior-specific logic PHẢI dùng `virtual`/`override`, KHÔNG dùng `if/switch` trên type | `[MUST]` |
| POL-02 | Strategy-based dispatch PHẢI qua Interface collection, KHÔNG qua Enum switch | `[MUST]` |
| POL-03 | Factory PHẢI return base type/interface, KHÔNG return concrete type | `[SHOULD]` |

---

## §3 — SOLID Principles — Concrete Rules

### 3.1 Single Responsibility Principle (SRP) `[MUST]`

**Quy tắc**: Mỗi class có **một và chỉ một lý do để thay đổi**.

```
Đúng SRP:
├── AssetService          → CRUD operations cho Asset
├── BulkAssetService      → Batch operations (reorder, bulk delete)
├── AssetCleanupHelper    → File cleanup logic
├── ThumbnailService      → Thumbnail generation & management
└── LocalStorageService   → Physical file I/O

Sai SRP (đã refactor):
├── AssetService (cũ)     → CRUD + Bulk + Cleanup + Storage (God Service)
```

**Metrics:**
| Metric | Threshold | Action |
|--------|-----------|--------|
| Lines of Code per class | ≤ 300 LOC | `[SHOULD]` split nếu vượt |
| Methods per class | ≤ 15 methods | `[SHOULD]` split nếu vượt |
| Constructor parameters | ≤ 5 dependencies | `[MUST]` refactor nếu vượt |
| Cyclomatic complexity per method | ≤ 10 | `[SHOULD]` extract methods |

### 3.2 Open/Closed Principle (OCP) `[MUST]`

**Quy tắc**: Open for extension, closed for modification.

```csharp
// ✅ ĐÚNG — Thêm filter mới KHÔNG cần sửa SmartCollectionService
// Chỉ cần tạo class mới implement ISmartCollectionFilter
public class FavoriteFilter : ISmartCollectionFilter
{
    public string FilterType => "favorites";
    public IQueryable<Asset> ApplyFilter(IQueryable<Asset> query, ...) => ...;
}

// Rồi register trong DI:
services.AddScoped<ISmartCollectionFilter, FavoriteFilter>();
// SmartCollectionService tự động nhận filter mới qua IEnumerable<ISmartCollectionFilter>

// ❌ SAI — Thêm filter mới phải sửa switch statement
switch (filterType)
{
    case "recent": return ApplyRecentFilter(query);
    case "untagged": return ApplyUntaggedFilter(query);
    case "favorites": return ApplyFavoriteFilter(query);  // Phải sửa file này!
}
```

**Rules:**
| ID | Rule | Severity |
|----|------|----------|
| OCP-01 | Thêm behavior mới PHẢI qua new class (implement interface), KHÔNG sửa existing class | `[MUST]` |
| OCP-02 | `IEnumerable<IInterface>` injection cho extensible collections | `[MUST]` |
| OCP-03 | Enum-based switch PHẢI có default case throw `NotSupportedException` | `[MUST]` |

### 3.3 Liskov Substitution Principle (LSP) `[MUST]`

**Quy tắc**: Subtype PHẢI thay thế được base type mà không thay đổi program correctness.

```csharp
// ✅ ĐÚNG — Mọi Asset subtype hoạt động đúng khi xử lý như Asset
List<Asset> assets = await _context.Assets.Where(a => a.UserId == userId).ToListAsync();
foreach (var asset in assets)
{
    // Polymorphic behavior — mỗi subtype tự biết mình có file hay không
    if (asset.RequiresFileCleanup)
        await CleanupFiles(asset);
    
    yield return asset.ToDto();  // Mỗi subtype map đúng DTO
}

// ❌ SAI — Type check phá vỡ LSP
if (asset is ImageAsset img)
    DoImageStuff(img);
else if (asset is LinkAsset link)
    DoLinkStuff(link);  // Nếu thêm type mới, quên case → bug
```

### 3.4 Interface Segregation Principle (ISP) `[MUST]`

**Quy tắc**: Client KHÔNG bị forced depend on methods nó không dùng.

```csharp
// ✅ ĐÚNG — Segregated interfaces
public interface IStorageService          // 4 methods — file operations only
{
    Task<string> UploadFileAsync(IFormFile file, string folder);
    Task DeleteFileAsync(string filePath);
    string GetPublicUrl(string filePath);
    Task<bool> FileExistsAsync(string filePath);
}

public interface IThumbnailService        // 3 methods — thumbnail operations only
{
    Task<ThumbnailResult> GenerateAsync(string sourcePath, Guid assetId);
    Task DeleteThumbnailsAsync(Guid assetId);
    // ...
}

// ❌ SAI — Fat interface
public interface IFileService
{
    Task Upload(...);
    Task Delete(...);
    Task GenerateThumbnails(...);  // Không phải mọi client cần thumbnails
    Task SendNotification(...);    // Hoàn toàn không liên quan!
}
```

**Rules:**
| ID | Rule | Severity |
|----|------|----------|
| ISP-01 | Interface tối đa 7 methods | `[SHOULD]` split nếu vượt |
| ISP-02 | Interface methods PHẢI cohesive (cùng domain concern) | `[MUST]` |
| ISP-03 | Nếu class implement interface nhưng throw `NotImplementedException` → ISP violation | `[MUST]` |

### 3.5 Dependency Inversion Principle (DIP) `[MUST]`

**Quy tắc**: High-level modules KHÔNG depend on low-level modules. Cả hai depend on abstractions.

```
Layer Dependency Direction (Clean Architecture):
    
    Controller → IService (interface)
         ↑
    Service implements IService, depends on → IRepository / DbContext
         ↑
    Infrastructure (EF Core, Redis, Local Storage)

    ⚠️  KHÔNG BAO GIỜ: Controller → Service (concrete)
    ⚠️  KHÔNG BAO GIỜ: Service → Controller
```

---

## §4 — Clean Architecture Layers

### 4.1 Layer Definition

```
┌─────────────────────────────────────────────────────┐
│                   PRESENTATION                      │
│  Controllers, Hubs, Middleware                      │
│  Allowed: IService, DTOs, Request/Response models   │
│  Forbidden: DbContext, EF queries, Business logic   │
├─────────────────────────────────────────────────────┤
│                   APPLICATION                       │
│  Services (IAssetService, ICollectionService, ...)  │
│  CQRS Handlers, Application Services                │
│  Allowed: Interfaces, Domain models, DTOs           │
│  Forbidden: HTTP concerns, Controller references    │
├─────────────────────────────────────────────────────┤
│                     DOMAIN                          │
│  Entities (Asset, Collection, Tag, ...)             │
│  Value Objects, Enums, Factory                      │
│  Allowed: Pure C# (no framework dependencies)       │
│  Forbidden: EF attributes*, HTTP, DI                │
├─────────────────────────────────────────────────────┤
│                 INFRASTRUCTURE                      │
│  AppDbContext, Migrations, LocalStorageService      │
│  Redis configuration, External integrations         │
│  Allowed: Framework dependencies, concrete impls    │
│  Forbidden: Business logic, Controller references   │
└─────────────────────────────────────────────────────┘

* Ngoại lệ: EF Core conventions qua Fluent API trong DbContext
  (không dùng [Required], [MaxLength] trên entity — dùng Fluent API)
```

### 4.2 Layer Dependency Rules `[MUST]`

```
✅ Allowed Dependencies:
  Presentation      →  Application  →  Domain
  Infrastructure    →  Domain
  
❌ Forbidden Dependencies:
  Domain        →  Application (domain KHÔNG biết application)
  Domain        →  Infrastructure (domain KHÔNG biết EF Core)
  Application   →  Presentation (service KHÔNG biết controller)
  Any layer     →  Presentation (chỉ entry point)
```

### 4.3 Current VAH Mapping

| Layer | Hiện tại (VAH) | Tương lai (sau refactor) |
|-------|----------------|--------------------------|
| **Presentation** | `Controllers/`, `Hubs/`, `Middleware/` | Giữ nguyên |
| **Application** | `Services/`, `CQRS/`, `Features/` | Tách thành `Application/` project |
| **Domain** | `Models/` | Tách thành `Domain/` project |
| **Infrastructure** | `Data/`, `Services/LocalStorage*`, `Migrations/` | Tách thành `Infrastructure/` project |

---

## §5 — Interface Design Standards

### 5.1 Naming Convention `[MUST]`

```csharp
// Prefix "I" + Domain Noun + "Service" / "Repository" / "Handler"
public interface IAssetService { }           // ✅ Application service
public interface IStorageService { }         // ✅ Infrastructure abstraction
public interface ISmartCollectionFilter { }  // ✅ Strategy interface
public interface INotificationService { }    // ✅ Cross-cutting concern

// ❌ Sai naming
public interface AssetService { }            // Thiếu "I" prefix
public interface IDoStuff { }                // Vague — "DoStuff" không mô tả domain
public interface IAssetHelper { }            // "Helper" là code smell — refactor thành Service
```

### 5.2 Interface Contract Rules `[MUST]`

```csharp
/// <summary>
/// Defines the contract for managing Asset lifecycle operations.
/// </summary>
/// <remarks>
/// <para><b>Domain:</b> Core (Asset Management)</para>
/// <para><b>Implementations:</b> <see cref="AssetService"/></para>
/// <para><b>Consumers:</b> AssetLayoutController, BulkAssetsController</para>
/// <para><b>Dependencies required by impl:</b> AppDbContext, IStorageService, IThumbnailService</para>
/// </remarks>
public interface IAssetService
{
    /// <summary>
    /// Retrieves all assets for a user within an optional collection scope.
    /// </summary>
    /// <param name="userId">The authenticated user's ID (from JWT claim).</param>
    /// <param name="collectionId">Optional collection filter. Null = root assets.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Ordered list of assets as DTOs.</returns>
    /// <exception cref="NotFoundException">Thrown when collection does not exist.</exception>
    Task<List<AssetResponseDto>> GetAssetsAsync(
        string userId, 
        Guid? collectionId, 
        CancellationToken cancellationToken = default);
    
    // ... mỗi method PHẢI có XML doc như trên
}
```

**Rules:**
| ID | Rule | Severity |
|----|------|----------|
| IF-01 | Mọi interface method PHẢI có `CancellationToken` parameter (async methods) | `[MUST]` |
| IF-02 | Return type PHẢI là DTO, KHÔNG return Entity ra ngoài Application layer | `[MUST]` |
| IF-03 | Interface PHẢI có XML `<summary>`, `<remarks>` (Domain, Implementations, Consumers) | `[MUST]` |
| IF-04 | Mọi parameter PHẢI có `<param>` doc | `[MUST]` |
| IF-05 | Mọi exception PHẢI có `<exception>` doc | `[SHOULD]` |
| IF-06 | Interface KHÔNG chứa properties (trừ Strategy identity property như `FilterType`) | `[SHOULD]` |

### 5.3 Interface Catalog (VAH)

```
Core Domain Interfaces:
├── IAssetService                  → 15 methods │ Asset CRUD, upload, types
├── IBulkAssetService              → 4 methods  │ Batch operations
├── ICollectionService             → ~10 methods│ Collection CRUD, tree
├── ISmartCollectionService        → ~5 methods │ Dynamic collections
└── ISmartCollectionFilter         → 3 methods  │ Strategy for filter types (5 impls)

Supporting Domain Interfaces:
├── ITagService                    → ~8 methods │ Tag CRUD, asset-tag relations
├── ISearchService                 → ~3 methods │ Full-text search
└── IPermissionService             → ~6 methods │ RBAC permission checks

Generic/Infrastructure Interfaces:
├── IStorageService                → 4 methods  │ File I/O abstraction
├── IThumbnailService              → ~3 methods │ Image processing
├── INotificationService           → ~2 methods │ SignalR push
└── IAuthService (implicit)        → ~3 methods │ JWT auth
```

---

## §6 — Abstract Class Standards

### 6.1 When to Use Abstract Class vs Interface `[MUST]`

| Criteria | Interface | Abstract Class |
|----------|-----------|----------------|
| Chỉ contract (no shared code) | ✅ | ❌ |
| Shared behavior + contract | ❌ | ✅ |
| Multiple "inheritance" | ✅ (một class impl nhiều interfaces) | ❌ (C# single inheritance) |
| Default implementation | ✅ (C# 8+ default interface methods — KHÔNG dùng) | ✅ |
| State (fields) | ❌ | ✅ |

> **VAH Policy `[MUST]`**: KHÔNG dùng default interface methods (C# 8+). Dùng abstract class nếu cần shared behavior.

### 6.2 Abstract Class Examples (VAH)

```csharp
// ✅ Abstract base entity — shared identity + audit
public abstract class Asset
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string UserId { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    
    // Virtual — subclass override để định nghĩa behavior
    public virtual bool HasPhysicalFile => false;
    public virtual bool CanHaveThumbnails => false;
    public virtual bool RequiresFileCleanup => HasPhysicalFile;
    
    // Template Method pattern — shared logic, extensible
    public AssetResponseDto ToDto() { /* mapping logic */ }
}

// ✅ Abstract base controller — shared auth helper
[ApiController]
[Authorize]
public abstract class BaseApiController : ControllerBase
{
    protected string GetUserId() 
        => User.FindFirstValue(ClaimTypes.NameIdentifier) 
           ?? throw new UnauthorizedAccessException("User ID not found in claims.");
}
```

**Rules:**
| ID | Rule | Severity |
|----|------|----------|
| ABC-01 | Abstract class PHẢI có ít nhất 1 `abstract` hoặc `virtual` member | `[MUST]` |
| ABC-02 | Abstract class KHÔNG được instantiated trực tiếp | `[MUST]` |
| ABC-03 | Constructor của abstract class PHẢI là `protected` | `[SHOULD]` |
| ABC-04 | Abstract class nên có `sealed` subclasses (prevent deep hierarchy) | `[SHOULD]` |

---

## §7 — Dependency Injection Rules

### 7.1 Service Lifetime `[MUST]`

```csharp
// Trong ServiceCollectionExtensions.cs
public static IServiceCollection AddApplicationServices(this IServiceCollection services)
{
    // Scoped — one instance per HTTP request (default for services)
    services.AddScoped<IAssetService, AssetService>();
    services.AddScoped<ICollectionService, CollectionService>();
    services.AddScoped<ITagService, TagService>();
    
    // Scoped — strategy collection (all filters resolved per request)
    services.AddScoped<ISmartCollectionFilter, RecentDaysFilter>();
    services.AddScoped<ISmartCollectionFilter, ContentTypeFilter>();
    services.AddScoped<ISmartCollectionFilter, UntaggedFilter>();
    
    // Singleton — stateless utilities, configuration
    services.AddSingleton<ISystemClock, SystemClock>();
    
    // Transient — lightweight, no state
    services.AddTransient<AssetCleanupHelper>();
    
    return services;
}
```

**Rules:**
| ID | Rule | Severity |
|----|------|----------|
| DI-01 | Services with DbContext PHẢI là `Scoped` | `[MUST]` |
| DI-02 | Stateless utilities MAY be `Singleton` | `[MAY]` |
| DI-03 | KHÔNG inject `Scoped` service vào `Singleton` | `[MUST]` |
| DI-04 | DI registration PHẢI tập trung trong `ServiceCollectionExtensions` | `[MUST]` |
| DI-05 | KHÔNG dùng `ServiceLocator` pattern (resolve từ `IServiceProvider` trực tiếp) | `[MUST]` |
| DI-06 | Constructor tối đa 5 parameters — vượt thì refactor (Facade Service) | `[MUST]` |

### 7.2 Registration Organization

```csharp
// ServiceCollectionExtensions.cs — tổ chức theo concern
public static class ServiceCollectionExtensions
{
    // §7.2.1 — Cross-cutting
    public static IServiceCollection AddCorsPolicy(this IServiceCollection s, IConfiguration c) { }
    public static IServiceCollection AddRateLimitingPolicies(this IServiceCollection s) { }
    
    // §7.2.2 — Infrastructure
    public static IServiceCollection AddDatabase(this IServiceCollection s, IConfiguration c) { }
    public static IServiceCollection AddCachingServices(this IServiceCollection s, IConfiguration c) { }
    
    // §7.2.3 — Identity & Auth
    public static IServiceCollection AddIdentityAndAuth(this IServiceCollection s, IConfiguration c) { }
    
    // §7.2.4 — Application Services (domain logic)
    public static IServiceCollection AddApplicationServices(this IServiceCollection s) { }
}
```

---

## §8 — Design Patterns — When & Why

### 8.1 Pattern Catalog (Currently Used in VAH)

| Pattern | Áp dụng ở đâu | Vấn đề giải quyết | File(s) |
|---------|---------------|-------------------|---------|
| **Factory Method** | `AssetFactory` | Tạo đúng Asset subtype theo ContentType | `Models/AssetFactory.cs` |
| **Strategy** | `ISmartCollectionFilter` (5 impls) | Extensible filter logic, OCP-compliant | `Services/SmartCollectionFilters.cs` |
| **Template Method** | `Asset.ToDto()`, `Asset.HasPhysicalFile` | Shared mapping, type-specific behavior | `Models/Asset.cs` |
| **TPH Inheritance** | `Asset` → `ImageAsset`, `LinkAsset`, ... | Single table, polymorphic queries | `Models/AssetTypes.cs` |
| **Repository** *(planned)* | `IAssetRepository` | Decouple from EF Core DbContext | `Services/` → `Repositories/` |
| **CQRS** | `Commands/`, `Queries/` + MediatR | Separate read/write models | `CQRS/Assets/` |
| **Mediator** | MediatR `IRequest`/`IRequestHandler` | Decouple sender from handler | `CQRS/Assets/Handlers/` |
| **Observer** | SignalR `AssetHub` | Real-time client notifications | `Hubs/AssetHub.cs` |
| **Singleton** | `TokenManager` (Frontend) | Global JWT token state | `src/api/TokenManager.js` |
| **Facade** | `ServiceCollectionExtensions` | Simplify DI registration | `Extensions/` |
| **Extension Methods** | `AddDatabase()`, `AddIdentityAndAuth()` | Clean Program.cs, organized setup | `Extensions/` |

### 8.2 Pattern Selection Decision Tree

```
Cần tạo object phức tạp?
├── Nhiều variants cùng base type → Factory Method (AssetFactory)
├── Cấu hình phức tạp, nhiều optional params → Builder Pattern
└── Chỉ 1 type → Constructor trực tiếp

Cần thay đổi behavior runtime?
├── Behavior thay đổi theo type/config → Strategy (ISmartCollectionFilter)
├── Behavior thay đổi theo state → State Pattern
└── Wrap thêm behavior → Decorator Pattern

Cần decouple components?
├── Request-response, nhiều handlers → Mediator (MediatR)
├── Event broadcast, nhiều subscribers → Observer (SignalR)
└── Simplify complex subsystem → Facade

Cần organize data access?
├── Complex queries, separate read/write → CQRS
├── Abstract persistence → Repository
└── Simple CRUD → Service + DbContext (hiện tại)
```

### 8.3 Pattern Anti-patterns `[MUST]`

| Anti-pattern | Dấu hiệu | Cách sửa |
|-------------|-----------|----------|
| **God Service** | Service > 300 LOC, > 15 methods | Split by SRP (BulkAssetService đã tách) |
| **Service Locator** | `IServiceProvider.GetService<T>()` trong business logic | Constructor injection |
| **Anemic Domain** | Entity chỉ có properties, logic ở Service | Move behavior vào Entity |
| **Primitive Obsession** | `string userId` everywhere | Value Object: `UserId` (future) |
| **Feature Envy** | Service A thường xuyên gọi Service B's data | Merge hoặc extract shared interface |

---

## §9 — Entity & Domain Model Conventions

### 9.1 Entity Structure `[MUST]`

```csharp
/// <summary>
/// [1-sentence mô tả entity]
/// </summary>
/// <remarks>
/// <para><b>Table:</b> Assets (TPH)</para>
/// <para><b>Discriminator:</b> ContentType</para>
/// <para><b>Relationships:</b> User (N:1), Collection (N:1), Tags (M:N via AssetTag)</para>
/// <para><b>Invariants:</b> Name cannot be empty. Position must be >= 0.</para>
/// </remarks>
public class Asset  // hoặc abstract class
{
    // ═══════════════════════════════════════════
    // §9.1.1 — Identity
    // ═══════════════════════════════════════════
    public Guid Id { get; private set; } = Guid.NewGuid();
    
    // ═══════════════════════════════════════════
    // §9.1.2 — Scalar Properties (alphabetical)
    // ═══════════════════════════════════════════
    public string ContentType { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public string Name { get; private set; } = string.Empty;
    
    // ═══════════════════════════════════════════
    // §9.1.3 — Foreign Keys
    // ═══════════════════════════════════════════
    public Guid? CollectionId { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    
    // ═══════════════════════════════════════════
    // §9.1.4 — Navigation Properties
    // ═══════════════════════════════════════════
    public virtual Collection? Collection { get; private set; }
    public virtual ApplicationUser User { get; private set; } = null!;
    public virtual ICollection<AssetTag> AssetTags { get; private set; } = new List<AssetTag>();
    
    // ═══════════════════════════════════════════
    // §9.1.5 — Virtual Behavior Properties
    // ═══════════════════════════════════════════
    public virtual bool HasPhysicalFile => false;
    public virtual bool CanHaveThumbnails => false;
    
    // ═══════════════════════════════════════════
    // §9.1.6 — Domain Methods
    // ═══════════════════════════════════════════
    public void UpdatePosition(int x, int y) { ... }
    public void MoveToCollection(Guid? collectionId) { ... }
    public bool IsOwnedBy(string userId) => UserId == userId;
    
    // ═══════════════════════════════════════════
    // §9.1.7 — Mapping Methods
    // ═══════════════════════════════════════════
    public AssetResponseDto ToDto() { ... }
}
```

### 9.2 Enum Design `[MUST]`

```csharp
// ✅ Type-safe enum với explicit DB mapping
public enum AssetContentType
{
    Image,
    Link,
    Color,
    ColorGroup,
    Folder
}

// Bidirectional mapping cho backward-compatible DB strings
public static class EnumMappings
{
    private static readonly Dictionary<AssetContentType, string> ContentTypeToString = new()
    {
        [AssetContentType.Image] = "image",
        [AssetContentType.Link] = "link",
        // ...
    };
    
    public static string ToDbString(this AssetContentType type) 
        => ContentTypeToString[type];
    
    public static AssetContentType ToAssetContentType(this string dbValue)
        => StringToContentType[dbValue];
}
```

---

## §10 — Service Layer Conventions

### 10.1 Service Structure `[MUST]`

```csharp
/// <summary>
/// [Mô tả service — 1 sentence]
/// </summary>
/// <remarks>
/// <para><b>Interface:</b> <see cref="IAssetService"/></para>
/// <para><b>Domain:</b> Core (Asset Management)</para>
/// <para><b>Dependencies:</b></para>
/// <list type="bullet">
///   <item><see cref="AppDbContext"/> — Data persistence</item>
///   <item><see cref="IStorageService"/> — File operations</item>
///   <item><see cref="IThumbnailService"/> — Image processing</item>
///   <item><see cref="INotificationService"/> — Real-time updates</item>
///   <item><see cref="ILogger{T}"/> — Structured logging</item>
/// </list>
/// </remarks>
public class AssetService : IAssetService
{
    // ═══════════════════════════════════════════
    // Fields — readonly, private, injected via constructor
    // ═══════════════════════════════════════════
    private readonly AppDbContext _context;
    private readonly IStorageService _storageService;
    private readonly IThumbnailService _thumbnailService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<AssetService> _logger;
    
    // ═══════════════════════════════════════════
    // Constructor — DI only, no logic
    // ═══════════════════════════════════════════
    public AssetService(
        AppDbContext context,
        IStorageService storageService,
        IThumbnailService thumbnailService,
        INotificationService notificationService,
        ILogger<AssetService> logger)
    {
        _context = context;
        _storageService = storageService;
        _thumbnailService = thumbnailService;
        _notificationService = notificationService;
        _logger = logger;
    }
    
    // ═══════════════════════════════════════════
    // Public methods — implement interface, ordered same as interface
    // ═══════════════════════════════════════════
    
    /// <inheritdoc />
    public async Task<List<AssetResponseDto>> GetAssetsAsync(
        string userId, 
        Guid? collectionId, 
        CancellationToken cancellationToken = default)
    {
        // 1. Validate input
        // 2. Query with Include
        // 3. Map to DTO
        // 4. Return
    }
    
    // ═══════════════════════════════════════════
    // Private helper methods — extract complex logic
    // ═══════════════════════════════════════════
    private async Task CleanupPhysicalFiles(Asset asset) { ... }
}
```

### 10.2 Service Rules

| ID | Rule | Severity |
|----|------|----------|
| SVC-01 | Service PHẢI implement exactly 1 interface | `[MUST]` |
| SVC-02 | All dependencies qua constructor injection (readonly fields) | `[MUST]` |
| SVC-03 | Public methods = interface methods only | `[MUST]` |
| SVC-04 | Business logic trong Service, KHÔNG trong Controller | `[MUST]` |
| SVC-05 | Service KHÔNG throw HTTP-specific exceptions (no `BadRequestException`) | `[SHOULD]` |
| SVC-06 | Dùng `CancellationToken` trên mọi async method | `[MUST]` |
| SVC-07 | Log ở level Info cho operations, Warning cho edge cases, Error cho failures | `[SHOULD]` |

---

## §11 — CQRS & MediatR Conventions

### 11.1 Command/Query Separation `[MUST]`

```csharp
// ═══════════════════════════════════════════
// QUERIES — Read operations (idempotent, no side effects)
// ═══════════════════════════════════════════
/// <summary>
/// Query to retrieve all assets for a user in a collection.
/// </summary>
public record GetAssetsQuery(
    string UserId, 
    Guid? CollectionId
) : IRequest<List<AssetResponseDto>>;

// ═══════════════════════════════════════════
// COMMANDS — Write operations (mutate state)
// ═══════════════════════════════════════════
/// <summary>
/// Command to upload one or more asset files.
/// </summary>
public record UploadAssetsCommand(
    string UserId,
    Guid? CollectionId,
    List<IFormFile> Files
) : IRequest<List<AssetResponseDto>>;
```

### 11.2 Handler Structure `[MUST]`

```csharp
/// <summary>
/// Handles <see cref="GetAssetsQuery"/>.
/// </summary>
public class GetAssetsQueryHandler : IRequestHandler<GetAssetsQuery, List<AssetResponseDto>>
{
    private readonly AppDbContext _context;
    
    public GetAssetsQueryHandler(AppDbContext context) => _context = context;
    
    public async Task<List<AssetResponseDto>> Handle(
        GetAssetsQuery request, 
        CancellationToken cancellationToken)
    {
        return await _context.Assets
            .Where(a => a.UserId == request.UserId)
            .Where(a => a.CollectionId == request.CollectionId)
            .OrderBy(a => a.PositionY).ThenBy(a => a.PositionX)
            .Select(a => a.ToDto())
            .ToListAsync(cancellationToken);
    }
}
```

**Rules:**
| ID | Rule | Severity |
|----|------|----------|
| CQRS-01 | Queries PHẢI là `record` (immutable) | `[MUST]` |
| CQRS-02 | Commands PHẢI là `record` (immutable) | `[MUST]` |
| CQRS-03 | 1 Handler = 1 Query/Command (no multi-handle) | `[MUST]` |
| CQRS-04 | Handler KHÔNG gọi other Handler trực tiếp | `[MUST]` |
| CQRS-05 | File naming: `{Entity}Queries.cs`, `{Entity}Commands.cs`, `{Entity}QueryHandlers.cs` | `[MUST]` |

---

## §12 — Exception Handling Architecture

### 12.1 Exception Hierarchy `[MUST]`

```
Exception (System)
├── NotFoundException               → 404 Not Found
│   "Collection with ID {id} not found"
├── ValidationException             → 400 Bad Request
│   "Asset name cannot be empty"
├── UnauthorizedAccessException     → 401/403
│   "User does not have permission to access this collection"
└── InvalidOperationException       → 500 (unexpected)
    "Database state inconsistency detected"
```

### 12.2 Exception Flow

```
Service throws NotFoundException
    ↓
ExceptionHandlingMiddleware catches
    ↓
Maps to ProblemDetails (RFC 7807)
    ↓
Returns HTTP 404 with structured JSON
```

```csharp
// ✅ ĐÚNG — Service throws domain exception
public async Task<AssetResponseDto> GetAssetByIdAsync(string userId, Guid id, CancellationToken ct)
{
    var asset = await _context.Assets.FindAsync(new object[] { id }, ct)
        ?? throw new NotFoundException($"Asset with ID {id} not found");
    
    if (!asset.IsOwnedBy(userId))
        throw new UnauthorizedAccessException("Access denied");
    
    return asset.ToDto();
}

// ❌ SAI — Service returns HTTP status (coupling to Presentation)
public async Task<IActionResult> GetAssetByIdAsync(...)
{
    return NotFound();  // Service không nên biết về HTTP!
}
```

---

## §13 — Frontend OOP Conventions (React 19)

### 13.1 API Layer — Class-based OOP `[MUST]`

```javascript
/**
 * @abstract
 * @class BaseApiService
 * @description Base class for all API service modules.
 * Provides shared HTTP methods (get, post, put, delete) with auth headers.
 * 
 * @property {AxiosInstance} client - Configured Axios instance
 * 
 * @example
 * class AssetsApi extends BaseApiService {
 *   getAssets(collectionId) {
 *     return this.get('/assets', { params: { collectionId } });
 *   }
 * }
 */
export class BaseApiService {
    constructor(client) {
        if (new.target === BaseApiService) {
            throw new Error('BaseApiService is abstract — cannot instantiate directly');
        }
        this.client = client;
    }
    
    async get(url, config) { return this.client.get(url, config); }
    async post(url, data, config) { return this.client.post(url, data, config); }
    async put(url, data, config) { return this.client.put(url, data, config); }
    async delete(url, config) { return this.client.delete(url, config); }
}
```

### 13.2 Singleton Pattern — TokenManager `[MUST]`

```javascript
/**
 * @class TokenManager
 * @description Singleton managing JWT token lifecycle.
 * Single source of truth for access/refresh tokens.
 * 
 * @pattern Singleton
 * @invariant Only one instance exists per application lifecycle.
 */
class TokenManager {
    #accessToken = null;   // Private field (ES2022)
    #refreshToken = null;
    
    static #instance = null;
    
    static getInstance() {
        if (!TokenManager.#instance) {
            TokenManager.#instance = new TokenManager();
        }
        return TokenManager.#instance;
    }
}
```

### 13.3 Custom Hooks — Separation of Concerns `[MUST]`

```
Custom Hooks (functional, nhưng tuân thủ SRP):
├── useAuth.js               → Authentication state & actions
├── useAssets.js             → Asset CRUD operations
├── useAssetSelection.js     → Multi-select state management
├── useBulkOperations.js     → Batch actions
├── useCollections.js        → Collection CRUD
├── useSignalR.js            → WebSocket connection lifecycle
├── useTags.js               → Tag operations
└── useUndoRedo.js           → Command history (Command Pattern)
```

**Rules:**
| ID | Rule | Severity |
|----|------|----------|
| FE-01 | API layer PHẢI dùng class-based inheritance (`extends BaseApiService`) | `[MUST]` |
| FE-02 | Global state (tokens) PHẢI qua Singleton class | `[MUST]` |
| FE-03 | 1 hook = 1 concern (SRP for hooks) | `[MUST]` |
| FE-04 | Hook KHÔNG gọi API trực tiếp — phải qua API service class | `[MUST]` |
| FE-05 | Domain models PHẢI là class với validation methods | `[SHOULD]` |
| FE-06 | Components KHÔNG chứa business logic — delegate to hooks | `[MUST]` |

---

## §14 — XML Documentation Standards (.NET 9)

### 14.1 Required Tags `[MUST]`

```csharp
/// <summary>
/// [BẮT BUỘC] Mô tả ngắn gọn (1-2 câu).
/// </summary>
/// <remarks>
/// [BẮT BUỘC cho public class/interface]
/// <para><b>Domain:</b> Core | Supporting | Generic</para>
/// <para><b>Pattern:</b> Strategy | Factory | Repository | ...</para>
/// <para><b>Dependencies:</b> List dependencies</para>
/// </remarks>
/// <param name="paramName">[BẮT BUỘC] Mô tả parameter</param>
/// <returns>[BẮT BUỘC cho non-void] Mô tả return value</returns>
/// <exception cref="ExceptionType">[NÊN CÓ] Khi nào throw</exception>
/// <example>
/// [TÙY CHỌN] Code example
/// <code>
/// var result = await service.GetAssetAsync(userId, id);
/// </code>
/// </example>
/// <seealso cref="RelatedType"/>
```

### 14.2 Coverage Requirements

| Element | Required Tags | Severity |
|---------|--------------|----------|
| `public interface` | `<summary>`, `<remarks>` (Domain, Implementations, Consumers) | `[MUST]` |
| `public class` | `<summary>`, `<remarks>` (Domain, Pattern, Dependencies) | `[MUST]` |
| `public method` | `<summary>`, `<param>`, `<returns>`, `<exception>` | `[MUST]` |
| `public property` | `<summary>` | `[SHOULD]` |
| `private method` | `<summary>` (nếu > 10 LOC) | `[SHOULD]` |
| `enum` | `<summary>` per value | `[SHOULD]` |

### 14.3 Automation Setup

```xml
<!-- VAH.Backend.csproj — Enable XML doc generation -->
<PropertyGroup>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);1591</NoWarn> <!-- Suppress missing XML doc warnings initially -->
</PropertyGroup>
```

```bash
# Generate API documentation from XML comments
dotnet tool install -g docfx
docfx init -q
docfx build
```

---

## §15 — JSDoc Standards (React 19)

### 15.1 Required Tags `[MUST]`

```javascript
/**
 * @module ModuleName
 * @description [BẮT BUỘC] Mô tả module
 */

/**
 * @class ClassName
 * @extends ParentClass
 * @description [BẮT BUỘC] Mô tả class
 * @pattern [NÊN CÓ] Design pattern used
 * 
 * @param {Type} paramName - [BẮT BUỘC] Mô tả parameter
 * @returns {Type} [BẮT BUỘC cho non-void] Mô tả return
 * @throws {ErrorType} [NÊN CÓ] Khi nào throw
 * 
 * @example
 * // [TÙY CHỌN] Usage example
 * const api = new AssetsApi(client);
 * const assets = await api.getAssets(collectionId);
 */
```

### 15.2 Hook Documentation

```javascript
/**
 * @hook useAssets
 * @description Manages asset CRUD operations and state.
 * 
 * @param {Object} options
 * @param {string} options.collectionId - Active collection filter
 * 
 * @returns {Object} Asset operations and state
 * @returns {Asset[]} returns.assets - Current asset list
 * @returns {Function} returns.uploadAssets - Upload handler
 * @returns {Function} returns.deleteAsset - Delete handler
 * @returns {boolean} returns.isLoading - Loading state
 * 
 * @dependency {AssetsApi} assetsApi - API service instance
 * @dependency {useSignalR} signalR - Real-time updates
 * 
 * @example
 * const { assets, uploadAssets, isLoading } = useAssets({ collectionId });
 */
export function useAssets({ collectionId }) { ... }
```

### 15.3 Automation Setup

```json
// package.json
{
  "scripts": {
    "docs": "jsdoc src/ -r -d docs/generated",
    "docs:watch": "jsdoc src/ -r -d docs/generated --watch"
  },
  "devDependencies": {
    "jsdoc": "^4.0.0",
    "better-docs": "^2.7.0"
  }
}
```

---

## §16 — Anti-Patterns & Violations

### 16.1 Forbidden Patterns `[MUST]`

| # | Anti-pattern | Dấu hiệu | Hậu quả | Rule ID |
|---|-------------|-----------|---------|---------|
| 1 | **God Class** | > 300 LOC, > 15 methods, > 5 dependencies | Untestable, hard to change | SRP violation |
| 2 | **Service Locator** | `provider.GetService<T>()` in business code | Hidden dependencies | DIP violation |
| 3 | **Anemic Domain** | Entity = data bag, all logic in Service | Scattered business rules | ENC violation |
| 4 | **Leaky Abstraction** | Return `DbSet<T>` or `IQueryable<T>` from Service | Couples consumer to EF | ABS violation |
| 5 | **Magic Strings** | `if (contentType == "image")` | Typo → runtime bug | Use Enum |
| 6 | **Circular Dependency** | Service A → Service B → Service A | Stack overflow, design smell | Extract interface |
| 7 | **Static Abuse** | `static class BusinessLogic` | Untestable, global state | Use DI instead |
| 8 | **Exception Swallowing** | `catch { }` or `catch { return null; }` | Silent failures | Log + rethrow |
| 9 | **Controller Logic** | Business rules in Controller | Duplicated logic | Move to Service |
| 10 | **Direct `new` in Service** | `var svc = new OtherService()` | Untestable, hidden dependency | Use DI |

### 16.2 Detection & Prevention

```
Code Review Checklist (mỗi PR):
□ Không có class > 300 LOC?
□ Không có method > 50 LOC?
□ Không có constructor > 5 params?
□ Mọi public class/interface có XML doc?
□ Không có Service Locator pattern?
□ Không có business logic trong Controller?
□ Mọi exception được log hoặc rethrow?
□ Mọi async method có CancellationToken?
□ Không có magic strings?
□ Mọi dependency qua constructor injection?
```

---

## §17 — Compliance Checklist

### 17.1 New File Checklist

```
Khi tạo file mới, PHẢI kiểm tra:

□ Interface
  ├── Tên đúng convention: I{Domain}{Role} (IAssetService)
  ├── XML doc: <summary>, <remarks> (Domain, Implementations, Consumers)
  ├── Mỗi method có <param>, <returns>, <exception>
  ├── CancellationToken trên mọi async method
  └── Return DTO (KHÔNG return Entity)

□ Class (Service)
  ├── Implement exactly 1 interface
  ├── Constructor injection only (readonly fields)
  ├── XML doc: <summary>, <remarks> (Domain, Pattern, Dependencies)
  ├── ≤ 300 LOC, ≤ 15 methods, ≤ 5 constructor params
  └── Registered trong ServiceCollectionExtensions

□ Entity (Domain Model)
  ├── Private setters on all properties
  ├── Domain methods cho state mutations
  ├── XML doc: <summary>, <remarks> (Table, Relationships, Invariants)
  ├── Virtual behavior properties (nếu polymorphic)
  └── ToDto() mapping method

□ Controller
  ├── Inherits BaseApiController
  ├── Thin — chỉ gọi Service methods
  ├── [Authorize] attribute
  ├── Proper HTTP status codes
  └── XML doc: <summary> per action

□ React Component
  ├── JSDoc: @module, @description
  ├── Logic delegate to custom hook
  ├── No direct API calls
  └── Props documented with @param

□ React API Service
  ├── Extends BaseApiService
  ├── JSDoc: @class, @extends, @description
  ├── Mỗi method có @param, @returns
  └── Export singleton instance
```

### 17.2 Quarterly Audit Metrics

| Metric | Target | Tool |
|--------|--------|------|
| XML doc coverage | > 80% | `docfx` warnings |
| JSDoc coverage | > 70% | `jsdoc` warnings |
| Max LOC per class | ≤ 300 | Static analysis |
| Max constructor params | ≤ 5 | Code review |
| Interface-to-class ratio | > 0.3 | Manual count |
| Anti-pattern instances | 0 | Code review |

---

## §18 — Appendix: Decision Matrix

### 18.1 When to Create an Interface?

| Scenario | Create Interface? | Reason |
|----------|------------------|--------|
| Service with business logic | ✅ Yes | Testability, DIP |
| Helper/Utility class (stateless) | ❌ No | Overkill — direct injection OK |
| External dependency wrapper | ✅ Yes | Swappable implementation |
| Domain entity | ❌ No | Entity ≠ service, no substitution needed |
| Strategy variants | ✅ Yes | OCP, runtime dispatch |
| Data Transfer Object | ❌ No | DTO = data carrier, no behavior |

### 18.2 When to Use Abstract vs Concrete Base Class?

| Scenario | Abstract | Concrete |
|----------|----------|----------|
| Should never be instantiated directly | ✅ Abstract | ❌ |
| Has virtual members requiring override | ✅ Abstract | ❌ |
| Standalone useful (e.g., default config) | ❌ | ✅ Concrete |
| TPH base entity | ✅ Abstract (`Asset`) | ❌ |
| Controller base | ✅ Abstract (`BaseApiController`) | ❌ |

### 18.3 Service Lifetime Decision

| Question | Answer → Lifetime |
|----------|-------------------|
| Depends on DbContext? | Yes → `Scoped` |
| Stateless + lightweight? | Yes → `Transient` |
| Initialized once + thread-safe? | Yes → `Singleton` |
| Holds configuration only? | Yes → `Singleton` (via `IOptions<T>`) |
| Unsure? | → `Scoped` (safe default) |

---

## §19 — Architecture Governance Model

> **Source**: Migrated from `ARCHITECTURE_REVIEW.md` §3

### 19.1 Decision Authority

| Decision Type | Authority | Process | Record |
|---------------|-----------|---------|--------|
| Tactical (library update, config change) | Any developer | PR review | Commit message |
| Standard (new service, new entity, API change) | Tech lead | PR + design discussion | ADR in `docs/adr/` |
| Strategic (new domain, infra change, breaking API) | Principal + stakeholder | RFC → review → approve | ADR + architecture doc update |

### 19.2 ADR Format

**Location:** `docs/03_ARCHITECTURE/ADR/ADR-NNN_TITLE.md`

```
# ADR-NNN: [Title]
Date: YYYY-MM-DD
Status: Proposed | Accepted | Deprecated | Superseded by ADR-XXX
Context: [Why this decision is needed]
Decision: [What was decided]
Consequences: [Trade-offs accepted]
```

**When to write an ADR:**
- Adding or removing a major dependency
- Changing data model relationships
- Introducing a new architectural pattern
- Modifying API contracts with breaking changes
- Changing deployment topology

### 19.3 Breaking Change Policy

| Scope | Policy | Notice Period |
|-------|--------|---------------|
| API endpoint removal | Deprecate → alias → remove after 2 releases | 2 sprints minimum |
| API response shape change | Additive only (no field removal) | None for additions |
| SignalR event rename/remove | Coordinate with frontend before merge | Same release cycle |
| Database migration (destructive) | Backup required, staging validation mandatory | 1 sprint |
| Environment variable rename | Update all docker-compose profiles + docs | Same release |

### 19.4 Review Cadence

| Activity | Frequency | Owner | Output |
|----------|-----------|-------|--------|
| Architecture doc review | Quarterly | Tech lead | Version bump, stale content pruned |
| Dependency audit | Quarterly | Any developer | Dependency Risk Matrix updated |
| Security posture review | Semi-annually | Tech lead | Threat model refresh |
| SLO review | Monthly (when monitoring exists) | Ops / Tech lead | SLO targets adjusted |
| Tech debt triage | Per sprint | Team | Debt items prioritized in backlog |

### 19.5 Codebase Health Governance Rules (GR1–GR10)

Measurable thresholds enforced via CI (where tooling exists) and manual audit quarterly. **Hard rules**, not guidelines.

| Rule | Metric | Threshold | Current Status |
|------|--------|-----------|----------------|
| GR1 | Max file LOC (any .cs/.jsx/.js) | ≤300 lines | ❌ Violated: AppContext 472, App.jsx 477, ColorBoard 555 |
| GR2 | Max cyclomatic complexity per method | ≤15 | Unknown — no tooling |
| GR3 | Service layer test coverage | ≥70% | ❌ 0% |
| GR4 | No `DbContext` in controllers | 0 direct usages | ✅ Compliant |
| GR5 | API response time P95 | <500ms | Unknown — no monitoring |
| GR6 | Max controller action methods | ≤10 per controller | ✅ Compliant |
| GR7 | Zero `TODO`/`HACK`/`FIXME` in main branch | 0 count | Unknown |
| GR8 | Dependency freshness | No dep >1 major behind | 🟡 .NET 9 STS → plan .NET 10 |
| GR9 | No destructive migration without rollback script | 100% compliance | ❌ No staging |
| GR10 | Frontend bundle size | <500KB gzipped | ✅ ~200KB |

**Violation Process:**
1. **New violations** → block PR merge (when CI enforced)
2. **Existing violations** → tracked in Technical Debt register with timeline
3. **Waiver** → requires ADR documenting why threshold is inappropriate
4. **Quarterly review** → thresholds adjusted if consistently too strict/lenient

---

> **Document End**  
> Next: [DESIGN_PRINCIPLES.md](DESIGN_PRINCIPLES.md) — SOLID & Clean Architecture rationale  
> Related: [PATTERN_CATALOG.md](PATTERN_CATALOG.md) — Full design pattern documentation
