# Quy Ước Kiến Trúc (Architecture Conventions)

> **Mục đích**: Tiêu chuẩn OOP bắt buộc cho VAH Backend (.NET 9) & Frontend (React 19)  
> **Last Updated**: 2026-04-06

---

## §1 — Mục Đích & Phạm Vi

### §1.1 Severity Levels

| Tag | Ý nghĩa | PR Action |
|-----|---------|-----------|
| `[MUST]` | Bắt buộc | Reject nếu vi phạm |
| `[SHOULD]` | Khuyến khích | Yêu cầu giải thích nếu bỏ qua |
| `[MAY]` | Tùy chọn | Suggestion only |

### §1.2 Phạm Vi Áp Dụng

```
VAH.Backend/          → §2-§12
src/VAH.Frontend/     → §2, §12
```

---

## §2 — OOP Pillars

### §2.1 Encapsulation `[MUST]`

**Nguyên tắc**: Class chỉ expose những gì cần thiết. Internal state PHẢI được bảo vệ.

```csharp
// ✅ ĐÚNG — Private setters, domain methods
public class Asset
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    
    public void UpdatePosition(int x, int y) { PositionX = x; PositionY = y; }
}

// ❌ SAI — Public setters bypass business rules
public int PositionX { get; set; }
```

**Rules:**
| ID | Rule | Severity |
|----|------|----------|
| ENC-01 | Entity properties: `private set` / `init` | `[MUST]` |
| ENC-02 | State mutation qua domain methods | `[MUST]` |
| ENC-03 | Collection navigation: `ICollection<T>` / `IReadOnlyCollection<T>` | `[MUST]` |

### §2.2 Abstraction `[MUST]`

```csharp
// ✅ ĐÚNG — Depend on interface
public AssetLayoutController(IAssetService assetService)

// ❌ SAI — Depend on concrete
public AssetLayoutController(AssetService assetService)
```

| ID | Rule | Severity |
|----|------|----------|
| ABS-01 | Service PHẢI có Interface | `[MUST]` |
| ABS-02 | Constructor injection dùng Interface | `[MUST]` |
| ABS-04 | Không `new` Service trong Service khác | `[MUST]` |

### §2.3 Inheritance `[SHOULD]`

**Nguyên tắc**: Prefer composition over inheritance. Chỉ dùng khi có "is-a" relationship.

```csharp
// ✅ TPH inheritance — clear "is-a"
public abstract class Asset { }
public class ImageAsset : Asset { public override bool HasPhysicalFile => true; }
public class LinkAsset : Asset { public override bool HasPhysicalFile => false; }

// ❌ SAI — Inheritance chỉ để reuse code
public class TagService : AssetService { }  // Tag không phải Asset!
```

| ID | Rule | Severity |
|----|------|----------|
| INH-01 | Max 2 levels: Base → Concrete | `[MUST]` |
| INH-02 | Base class PHẢI là `abstract` | `[MUST]` |

### §2.4 Polymorphism `[MUST]`

```csharp
// ✅ ĐÚNG — Virtual properties (TPH)
if (asset.HasPhysicalFile)
    await _storageService.DeleteAsync(asset.FilePath);

// ❌ SAI — Type check phá vỡ LSP
if (asset is ImageAsset img) DoImageStuff(img);
```

| ID | Rule | Severity |
|----|------|----------|
| POL-01 | Dùng `virtual`/`override`, KHÔNG `if/switch` trên type | `[MUST]` |
| POL-02 | Strategy dispatch qua Interface collection | `[MUST]` |

---

## §3 — SOLID Principles

### §3.1 SRP `[MUST]`

**Quy tắc**: Mỗi class có một lý do để thay đổi.

| Metric | Threshold |
|--------|-----------|
| LOC per class | ≤ 300 |
| Methods per class | ≤ 15 |
| Constructor params | ≤ 5 `[MUST]` |

### §3.2 OCP `[MUST]`

**Quy tắc**: Open for extension, closed for modification.

```csharp
// ✅ Thêm filter mới — tạo class mới
public class FavoriteFilter : ISmartCollectionFilter { ... }
services.AddScoped<ISmartCollectionFilter, FavoriteFilter>();

// ❌ Thêm filter — sửa switch statement
switch (filterType) { case "favorites": ... }
```

### §3.3 LSP `[MUST]`

**Quy tắc**: Subtype thay thế được base type.

### §3.4 ISP `[MUST]`

**Quy tắc**: Client không depend on methods nó không dùng.

| ID | Rule | Severity |
|----|------|----------|
| ISP-01 | Interface ≤ 7 methods | `[SHOULD]` |
| ISP-02 | Interface methods cohesive | `[MUST]` |

### §3.5 DIP `[MUST]`

```
Controller → IService → Infrastructure
     ↓
High-level → Abstractions ← Low-level
```

---

## §4 — Clean Architecture Layers

```
┌─────────────────────────────────────────┐
│        PRESENTATION                     │
│  Controllers, Hubs, Middleware          │
├─────────────────────────────────────────┤
│        APPLICATION                      │
│  Services, CQRS Handlers                │
├─────────────────────────────────────────┤
│          DOMAIN                         │
│  Entities, Value Objects, Factory       │
├─────────────────────────────────────────┤
│        INFRASTRUCTURE                   │
│  DbContext, Storage, Cache              │
└─────────────────────────────────────────┘
```

**Forbidden Dependencies:**
- Domain → Application/Infrastructure
- Application → Presentation

### §4.1 VAH Mapping

| Layer | Folder |
|-------|--------|
| Presentation | `Controllers/`, `Hubs/`, `Middleware/` |
| Application | `Services/`, `CQRS/`, `Features/*/Application/` |
| Domain | `Models/` |
| Infrastructure | `Data/`, `Migrations/` |

---

## §5 — Interface Standards

```csharp
public interface IAssetService
{
    /// <summary>Retrieves assets for a user.</summary>
    /// <param name="userId">Authenticated user's ID.</param>
    /// <exception cref="NotFoundException">Collection not found.</exception>
    Task<List<AssetResponseDto>> GetAssetsAsync(
        string userId, Guid? collectionId, CancellationToken ct = default);
}
```

| ID | Rule | Severity |
|----|------|----------|
| IF-01 | Async methods có `CancellationToken` | `[MUST]` |
| IF-02 | Return DTO, không Entity | `[MUST]` |
| IF-03 | XML doc: `<summary>`, `<remarks>` | `[MUST]` |

---

## §6 — Dependency Injection

```csharp
// Scoped — Services với DbContext
services.AddScoped<IAssetService, AssetService>();

// Singleton — Stateless utilities
services.AddSingleton<ISystemClock, SystemClock>();

// Transient — Lightweight
services.AddTransient<AssetCleanupHelper>();
```

| ID | Rule | Severity |
|----|------|----------|
| DI-01 | Services với DbContext = `Scoped` | `[MUST]` |
| DI-03 | Không inject Scoped vào Singleton | `[MUST]` |
| DI-06 | Constructor ≤ 5 params | `[MUST]` |

---

## §7 — Design Patterns

| Pattern | Áp dụng | File |
|---------|---------|------|
| Factory Method | `AssetFactory` | `Models/AssetFactory.cs` |
| Strategy | `ISmartCollectionFilter` | `Services/SmartCollectionFilters.cs` |
| Template Method | `Asset.HasPhysicalFile` | `Models/Asset.cs` |
| CQRS | Commands/Queries | `CQRS/Assets/` |
| Mediator | MediatR | Handlers |
| Observer | SignalR | `Hubs/AssetHub.cs` |

---

## §8 — Entity Conventions

```csharp
public class Asset
{
    // §8.1 Identity
    public Guid Id { get; private set; }
    
    // §8.2 Scalar Properties
    public string Name { get; private set; }
    
    // §8.3 Foreign Keys
    public Guid? CollectionId { get; private set; }
    
    // §8.4 Navigation Properties
    public virtual Collection? Collection { get; private set; }
    
    // §8.5 Virtual Behavior
    public virtual bool HasPhysicalFile => false;
    
    // §8.6 Domain Methods
    public void UpdatePosition(int x, int y) { ... }
}
```

---

## §9 — Service Conventions

| ID | Rule | Severity |
|----|------|----------|
| SVC-01 | Implement exactly 1 interface | `[MUST]` |
| SVC-02 | Constructor injection only | `[MUST]` |
| SVC-04 | Business logic trong Service | `[MUST]` |
| SVC-06 | `CancellationToken` trên async | `[MUST]` |

---

## §10 — CQRS Conventions

```csharp
// Query (read, idempotent)
public record GetAssetsQuery(string UserId, Guid? CollectionId) 
    : IRequest<List<AssetResponseDto>>;

// Command (write, mutate state)  
public record UploadAssetsCommand(string UserId, List<IFormFile> Files) 
    : IRequest<List<AssetResponseDto>>;
```

| ID | Rule | Severity |
|----|------|----------|
| CQRS-01 | Queries = `record` (immutable) | `[MUST]` |
| CQRS-03 | 1 Handler = 1 Query/Command | `[MUST]` |

---

## §11 — Exception Handling

```
NotFoundException           → 404 Not Found
ValidationException         → 400 Bad Request
UnauthorizedAccessException → 401/403
```

```csharp
// ✅ Service throws domain exception
var asset = await _context.Assets.FindAsync(id, ct)
    ?? throw new NotFoundException($"Asset {id} not found");

// ❌ Service returns HTTP status
return NotFound();  // Service không biết HTTP!
```

---

## §12 — Frontend Conventions

| ID | Rule | Severity |
|----|------|----------|
| FE-01 | API layer dùng class inheritance (`extends BaseApiService`) | `[MUST]` |
| FE-02 | Global state qua Singleton class | `[MUST]` |
| FE-03 | 1 hook = 1 concern (SRP) | `[MUST]` |
| FE-06 | Components không chứa business logic | `[MUST]` |

---

## §13 — Anti-Patterns (Forbidden)

| Anti-pattern | Dấu hiệu | Cách sửa |
|-------------|-----------|----------|
| God Class | > 300 LOC, > 15 methods | Split by SRP |
| Service Locator | `GetService<T>()` trong business | Constructor injection |
| Anemic Domain | Entity chỉ có properties | Move behavior vào Entity |
| Magic Strings | `if (type == "image")` | Use Enum |

---

## §14 — Governance Rules

| Rule | Metric | Threshold |
|------|--------|-----------|
| GR1 | Max file LOC | ≤300 |
| GR3 | Test coverage | ≥70% |
| GR4 | DbContext in controllers | 0 |
| GR6 | Max controller actions | ≤10 |
| GR10 | Frontend bundle | <500KB |

---

> **Document End**  
> Related: [DESIGN_PRINCIPLES.md](DESIGN_PRINCIPLES.md) · [PATTERN_CATALOG.md](PATTERN_CATALOG.md)
