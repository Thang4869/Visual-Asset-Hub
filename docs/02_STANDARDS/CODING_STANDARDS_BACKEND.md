# Tiêu Chuẩn Code Backend (Coding Standards — .NET 9 Backend)

> **Mục đích**: Định nghĩa quy tắc và chuẩn mực code cho backend .NET 9  
> **Last Updated**: 2026-04-06  
> **Áp dụng cho**: `VAH.Backend/`

---

## §1 — Tổ Chức File & Namespace

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

---

## §2 — Quy Ước Đặt Tên

| Thành phần | Quy ước | Ví dụ |
|------------|---------|-------|
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

---

## §3 — Quy Tắc Cấu Trúc Code

### Controller (Mỏng)
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

**Quy tắc**: Không chứa business logic. Sử dụng primary constructor cho DI. `CancellationToken` cho mọi async action. `ProducesResponseType` cho Swagger.

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

**Quy tắc**: Implement đúng 1 interface. Fields `readonly`. Throw exception có cấu trúc. Luôn trả về DTOs (không trả entity).

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

**Quy tắc**: Abstract base hoặc sealed subtypes. **Private setters** cho tất cả value properties — thay đổi chỉ qua domain methods (vd: `Rename()`, `Reorder()`, `AssignToGroup()`). Guard clauses qua `ArgumentException.ThrowIfNullOrWhiteSpace`. Khởi tạo qua static Factory. Không reference DTO trong domain — mapping thuộc service layer (`AssetMapper`).

**Ghi chú migration**: Codebase hiện tại còn một số entities và DTO-adjacent types chưa tuân theo pattern `private set` hoặc vẫn dùng DataAnnotations (vd: `VAH.Backend/Models/CollectionPermission.cs`). Ưu tiên áp dụng dần: tạo các refactor PR nhỏ để chuyển setters sang `private set` và di chuyển attribute-based constraints sang Fluent API configurations trong `Data/` (qua `IEntityTypeConfiguration<T>`). Reviewers có thể chấp nhận exceptions có documented justification cho đến khi hoàn thành migrations.

---

## §4 — Quy Tắc Async/Await

| Quy tắc | Ví dụ |
|---------|-------|
| Mọi async method nhận `CancellationToken ct` | `Task<T> GetAsync(..., CancellationToken ct = default)` |
| Truyền `ct` cho TẤT CẢ downstream calls | `await _context.Assets.ToListAsync(ct)` |
| Không dùng `.Result` hoặc `.Wait()` | Chỉ dùng `await` |
| `ConfigureAwait(false)` KHÔNG cần thiết | ASP.NET Core không có SynchronizationContext |

---

## §5 — Xử Lý Lỗi

```
Throw domain exceptions         → GlobalExceptionHandler map sang HTTP status:
  NotFoundException             → 404 ProblemDetails
  ValidationException           → 400 ProblemDetails with errors dict
  ArgumentException             → 400 ProblemDetails
  KeyNotFoundException          → 404 ProblemDetails
  UnauthorizedAccessException   → 401 ProblemDetails
  *                             → 500 ProblemDetails (ẩn detail trong prod)
```

---

## §6 — Yêu Cầu XML Documentation

```csharp
// BẮT BUỘC trên: public interfaces, classes, methods
/// <summary>One-line description.</summary>
/// <param name="id">Asset primary key.</param>
/// <returns>DTO representation of the asset.</returns>
/// <exception cref="NotFoundException">Asset not found.</exception>

// BẮT BUỘC remarks trên interfaces và services:
/// <remarks>
/// <para><b>Domain:</b> Core (Asset Management)</para>
/// <para><b>Pattern:</b> Strategy</para>
/// </remarks>
```

---

## §7 — Quy Ước EF Core

| Quy tắc | Lý do |
|---------|-------|
| Enum lưu dạng string qua converter | Backward-compatible, dễ đọc |
| Global query filter cho soft-delete (`IsDeleted`) | Tránh lộ data vô tình |
| `Include()` tường minh — không lazy loading | Ngăn N+1 |
| Migrations auto-applied khi startup (`db.Database.Migrate()`) | Tiện cho dev/staging — tắt trong prod |
| `CancellationToken` truyền cho tất cả EF async calls | Hủy request đúng cách |

---

> **Document End**
