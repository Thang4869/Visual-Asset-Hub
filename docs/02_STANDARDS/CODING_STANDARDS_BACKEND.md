# CODING STANDARDS — .NET 9 Backend

> **Last Updated**: 2026-03-13  
> **Applies to**: `VAH.Backend/`

---

## §1 — File & Namespace Organization

```
VAH.Backend/
├── Configuration/          → IOptions<T> config POCOs
├── Controllers/            → Thin API controllers (legacy modules)
├── CQRS/                   → MediatR commands, queries, handlers
├── Data/                   → AppDbContext, EF config
├── Exceptions/             → Domain exceptions (NotFoundException, ValidationException)
├── Extensions/             → ServiceCollectionExtensions (DI facade)
├── Features/               → Vertical slices (new modules)
│   └── Assets/
│       ├── Application/    → IAssetApplicationService, strategies
│       ├── Commands/       → AssetsCommandController
│       ├── Common/         → Route names, constants
│       ├── Contracts/      → Request DTOs
│       ├── Infrastructure/ → File mapping, user context
│       └── Queries/        → AssetsQueryController
├── Hubs/                   → SignalR hubs
├── Middleware/             → Exception handling, request pipeline
├── Migrations/             → EF Core migrations (auto-generated)
├── Models/                 → Domain entities, DTOs, enums
├── Services/               → Application services (legacy modules)
└── Properties/             → launchSettings.json
```

## §2 — Naming Conventions

| Element | Convention | Example |
|---------|-----------|---------|
| Namespace | `VAH.Backend.{Layer}.{Feature}` | `VAH.Backend.Features.Assets.Application` |
| Interface | `I{Noun}{Role}` | `IAssetService`, `ISmartCollectionFilter` |
| Class | `{Noun}{Role}` | `AssetService`, `LocalStorageService` |
| Abstract class | `{Noun}` (no suffix) | `Asset`, `BaseApiController` |
| Record (CQRS) | `{Verb}{Noun}{Command\|Query}` | `CreateAssetCommand`, `GetAssetsQuery` |
| Handler | `{Command\|Query}Handler` | `CreateAssetHandler`, `GetAssetsHandler` |
| DTO | `{Verb}{Noun}Dto`, `{Noun}ResponseDto` | `CreateAssetDto`, `AssetResponseDto` |
| Enum | `{Noun}{Type}` (PascalCase values) | `AssetContentType.Image` |
| Controller | `{Noun}Controller` | `CollectionsController`, `TagsController` |
| Extension | `{Target}Extensions` | `ServiceCollectionExtensions` |
| Test | `{Class}_{Method}_{Scenario}` | `AssetFactory_CreateImage_SetsCorrectType` |

## §3 — Code Structure Rules

### Controller (Thin)
```csharp
[Route("api/v1/[controller]")]
[Authorize]
[Produces("application/json")]
public class TagsController(ITagService tagService) : BaseApiController
{
    /// <summary>XML doc required for every action.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<Tag>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Tag>>> GetTags(CancellationToken ct)
        => Ok(await tagService.GetAllAsync(GetUserId(), ct));
}
```

**Rules**: No business logic. Primary constructor for DI. `CancellationToken` on every async action. `ProducesResponseType` for Swagger.

### Service
```csharp
public class AssetService : IAssetService
{
    private readonly AppDbContext _context;
    private readonly IStorageService _storageService;
    // ... max 5 dependencies

    public AssetService(AppDbContext context, IStorageService storageService, ...) { ... }

    /// <inheritdoc />
    public async Task<AssetResponseDto> GetByIdAsync(int id, string userId, CancellationToken ct = default)
    {
        var asset = await _context.Assets.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, ct)
            ?? throw new NotFoundException(nameof(Asset), id);
        return asset.ToDto();
    }
}
```

**Rules**: Implement exactly 1 interface. `readonly` fields. Structured exception throwing. Always return DTOs (never entities).

### Entity
```csharp
public abstract class Asset
{
    protected Asset() { }  // EF Core materialization
    public int Id { get; private set; }                    // Identity
    public string FileName { get; private set; }           // Private setters — always
    public int CollectionId { get; private set; }          // FKs
    public Collection? Collection { get; set; }            // Navigation (EF manages)
    public virtual bool HasPhysicalFile => true;           // Behavior
    public void Rename(string newName) { ... }             // Domain method = only mutation path
}
```

**Rules**: Abstract base or sealed subtypes. **Private setters** on all value properties — mutations only through domain methods (e.g., `Rename()`, `Reorder()`, `AssignToGroup()`). Guard clauses via `ArgumentException.ThrowIfNullOrWhiteSpace`. Construction via static Factory. No DTO references in domain — mapping belongs in service layer (`AssetMapper`).

## §4 — Async/Await Rules

| Rule | Example |
|------|---------|
| Every async method takes `CancellationToken ct` | `Task<T> GetAsync(..., CancellationToken ct = default)` |
| Pass `ct` to ALL downstream calls | `await _context.Assets.ToListAsync(ct)` |
| Never `.Result` or `.Wait()` | Use `await` only |
| `ConfigureAwait(false)` NOT needed | ASP.NET Core has no SynchronizationContext |

## §5 — Error Handling

```
Throw domain exceptions         → GlobalExceptionHandler maps to HTTP status:
  NotFoundException             → 404 ProblemDetails
  ValidationException           → 400 ProblemDetails with errors dict
  ArgumentException             → 400 ProblemDetails
  KeyNotFoundException          → 404 ProblemDetails
  UnauthorizedAccessException   → 401 ProblemDetails
  *                             → 500 ProblemDetails (detail hidden in prod)
```

## §6 — XML Documentation Requirements

```csharp
// REQUIRED on: public interfaces, classes, methods
/// <summary>One-line description.</summary>
/// <param name="id">Asset primary key.</param>
/// <returns>DTO representation of the asset.</returns>
/// <exception cref="NotFoundException">Asset not found.</exception>

// REQUIRED remarks on interfaces and services:
/// <remarks>
/// <para><b>Domain:</b> Core (Asset Management)</para>
/// <para><b>Pattern:</b> Strategy</para>
/// </remarks>
```

## §7 — EF Core Conventions

| Rule | Rationale |
|------|-----------|
| Enum stored as string via converter | Backward-compatible, human-readable |
| Global query filter for soft-delete (`IsDeleted`) | Prevent accidental data exposure |
| `Include()` explicitly — no lazy loading | N+1 prevention |
| Migrations auto-applied on startup (`db.Database.Migrate()`) | Dev/staging convenience — disable in prod |
| `CancellationToken` passed to all EF async calls | Proper request cancellation |

---

> **Document End**
