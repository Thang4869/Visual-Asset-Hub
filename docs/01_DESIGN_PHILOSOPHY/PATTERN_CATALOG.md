# Danh Mục Design Patterns (Pattern Catalog)

> **Mục đích**: Tổng hợp 22 design patterns đang sử dụng trong VAH  
> **Last Updated**: 2026-04-06

---

## §1 — Creational Patterns

### Factory Method — `AssetFactory`
- **File**: `Models/AssetFactory.cs`
- **Vấn đề**: Tạo đúng TPH subtype với validation nhất quán
- **Giải pháp**: Static methods (`CreateImage`, `CreateFile`, `CreateFolder`, `CreateColor`, `CreateLink`)
- **OCP**: Thêm asset type = thêm factory method + sealed subclass

### Abstract Factory — `IAssetDuplicateStrategyFactory`
- **File**: `Features/Assets/Application/Duplicate/`
- **Giải pháp**: Factory chọn strategy dựa trên `targetFolderId`

### Factory Method — `ApiErrors`
- **File**: `Controllers/ApiErrors.cs`
- **Giải pháp**: Static methods tạo ProblemDetails chuẩn hóa

---

## §2 — Structural Patterns

### Facade — `ServiceCollectionExtensions`
- **Vấn đề**: `Program.cs` sẽ có 200+ LOC nếu không tổ chức
- **Giải pháp**: 6 extension methods nhóm theo concern

### Facade — `AssetApplicationService`
- **Giải pháp**: Wrap `ISender` + `IUserContextProvider` + `AssetOptions`

### Adapter — `FileMapperService`
- **Giải pháp**: `IFormFile[]` → `IReadOnlyCollection<UploadedFileDto>`

---

## §3 — Behavioral Patterns

### Strategy — `ISmartCollectionFilter`
- **File**: `Services/SmartCollectionFilters.cs`
- **Implementations**: `RecentDaysFilter`, `ContentTypeFilter`, `UntaggedFilter`, `WithThumbnailsFilter`, `TagFilter`
- **OCP**: New filter = new class + DI registration

### Strategy — `IAssetDuplicateStrategy`
- `InPlaceDuplicateStrategy`, `TargetFolderDuplicateStrategy`

### Template Method — `Asset` Virtual Properties
- `virtual bool HasPhysicalFile`, `CanHaveThumbnails` — subtypes override

### Validator — `AssetValidator`
- Static validator với `[GeneratedRegex]` (zero-allocation)

### Mediator — MediatR CQRS Pipeline
- `IRequest<T>` records + `IRequestHandler<TReq, TRes>`

### Observer — SignalR `AssetHub`
- `NotificationService.NotifyAssetChanged()` → clients via `useSignalR`

---

## §4 — Architectural Patterns

### CQRS
- Separate `GetAssetsQuery` (read) / `CreateAssetCommand` (write)

### Modular Monolith (Vertical Slices)
- `Features/Assets/` chứa Commands, Queries, Application, Infrastructure

---

## §5 — Infrastructure Patterns

### Singleton — `TokenManager` (Frontend)
- Module-level instance với private `#storageKey` field

### Singleton — `DatabaseProviderInfo`
- `record DatabaseProviderInfo(string ProviderName)` registered singleton

### Rate Limiter — Fixed Window
- `Fixed` (100 req/min), `Upload` (20 req/min)

---

## §6 — Frontend Patterns

### Module Pattern — API Barrel Export
- Single barrel exports all API singletons

### Inheritance — `BaseApiService`
- `_get()`, `_post()`, `_put()`, `_patch()`, `_delete()` — 7 subclasses extend

### Context + Hook Pattern
- `AppContext` + 11 custom hooks (`useAssets`, `useCollections`, etc.)

---

## §7 — Hướng Dẫn Chọn Pattern

```
Cần tạo nhiều variants?
  └─→ Factory (AssetFactory)

Cần thuật toán thay thế được?
  └─→ Strategy (ISmartCollectionFilter)

Cần decouple sender/receiver?
  └─→ Mediator (MediatR) hoặc Observer (SignalR)

Cần shared behavior + type-specific overrides?
  └─→ Template Method (Asset virtual)

Cần đơn giản hóa subsystem phức tạp?
  └─→ Facade (ServiceCollectionExtensions)

Cần adapt interfaces không tương thích?
  └─→ Adapter (FileMapperService)
```

---

> **Document End**  
> Related: [ARCHITECTURE_CONVENTIONS.md](ARCHITECTURE_CONVENTIONS.md) · [DESIGN_PRINCIPLES.md](DESIGN_PRINCIPLES.md)
