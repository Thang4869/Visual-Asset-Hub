# Quy Ước Database (Database Conventions — EF Core & PostgreSQL)

> **Mục đích**: Định nghĩa quy tắc và chuẩn mực cho database với EF Core  
> **Last Updated**: 2026-04-06  
> **ORM**: Entity Framework Core 9  
> **Providers**: PostgreSQL 17 (production) / SQLite (development)

---

## §1 — Kiến Trúc Dual Provider

```csharp
// Configured in ServiceCollectionExtensions.AddDatabase()
var dbProvider = configuration.GetValue<string>("DatabaseProvider") ?? "SQLite";
// PostgreSQL for Docker/production, SQLite for local development
```

`DatabaseProviderInfo` record được inject như Singleton để phát hiện provider lúc runtime.

---

## §2 — Cấu Hình Entity

### Quy ước: Trạng thái hiện tại & migration

- Hiện tại: một số entities sử dụng DataAnnotations (`[Key]`, `[Required]`, `[MaxLength]`) — đây vẫn là trạng thái hiện tại đang được migration dần.
- Ưu tiên: Sử dụng Fluent API qua `IEntityTypeConfiguration<T>` trong `AppDbContext` để giữ domain types không phụ thuộc framework. Di chuyển entity-level validation/shape vào Fluent API là refactor được khuyến nghị (`[SHOULD]`).
- Ví dụ: `VAH.Backend/Models/CollectionPermission.cs` chứa `[Required]` attributes và nên được xem xét migration sang Fluent API (xem ghi chú migration bên dưới).

### Ghi chú migration

- Theo dõi task migration ngắn hạn để di chuyển entity DataAnnotations vào Fluent API configurations. Ưu tiên permission và identity-adjacent types, sau đó đến core domain entities. Giữ DTO DataAnnotations (request validation) cho đến khi áp dụng chiến lược validation tập trung (vd: FluentValidation).

### TPH Discriminator
```csharp
// Asset table — ContentType column as discriminator
// Configured automatically by EF Core from subclass hierarchy:
// Asset (base) → ImageAsset, LinkAsset, ColorAsset, ColorGroupAsset, FolderAsset
// Enum stored as lowercase string via EnumMappings
```

---

## §3 — Quy Ước Đặt Tên

| Thành phần | Quy ước | Ví dụ |
|------------|---------|-------|
| Table | Plural PascalCase (EF default) | `Assets`, `Collections`, `Tags` |
| Column | PascalCase (EF default) | `FileName`, `CreatedAt`, `ParentFolderId` |
| FK column | `{NavigationProperty}Id` | `CollectionId`, `UserId`, `GroupId` |
| Junction table | `{Entity1}{Entity2}` | `AssetTags` (auto M:N) |
| Index | `IX_{Table}_{Column}` | `IX_Assets_UserId` |
| Migration | `{Timestamp}_{Description}` | `20260225203615_InitialCreate` |

---

## §4 — Schema Hiện Tại (6 Tables)

```
┌─────────────────┐     ┌─────────────────┐    ┌──────────────────┐
│ AspNetUsers     │     │ Collections     │    │ Tags             │
│ (Identity)      │←───┐│ Id, Name, Type  │    │ Id, Name, Color  │
│ Id, Email, ...  │    ││ ParentId (self) │    │ NormalizedName   │
└─────────────────┘    ││ UserId (FK)     │    │ UserId (FK)      │
         │             ││ LayoutType      │    └────────┬─────────┘
         │             │└────────┬────────┘             │
         │             │         │                      │
         ▼             │         ▼                      ▼
┌──────────────────┐   │ ┌─────────────────┐    ┌──────────────────┐
│ Assets (TPH)     │───┘ │ CollectionPerm. │    │ AssetTags (M:N)  │
│ Id, FileName     │     │ Id, CollectionId│    │ AssetId (FK)     │
│ FilePath, Tags   │     │ UserId, Role    │    │ TagId (FK)       │
│ ContentType      │     │ GrantedByUserId │    │                  │
│ CollectionId FK  │     └─────────────────┘    └──────────────────┘
│ UserId FK        │
│ ParentFolderId   │ (self-ref FK)
│ GroupId          │
│ ThumbnailSm/Md/Lg│
└──────────────────┘
```

---

## §5 — Quy Tắc Migration

| Quy tắc | Chi tiết |
|---------|----------|
| Auto-migrate khi startup | `db.Database.Migrate()` trong `Program.cs` |
| Không sửa migrations đã generate | Tạo lại nếu cần |
| Đặt tên migration | Mô tả rõ: `AddTagSystem`, `AddThumbnailColumns` |
| Data fixups | Raw SQL trong `Program.cs` sau migrate (ContentType discriminator fix) |
| **Production**: Tắt auto-migrate | Dùng `dotnet ef database update` thủ công |

---

## §6 — Mẫu Query

```csharp
// ✅ Luôn filter theo UserId (data isolation)
var assets = await _context.Assets
    .Where(a => a.UserId == userId)
    .OrderBy(a => a.SortOrder)
    .ToListAsync(ct);

// ✅ Dùng Include() tường minh (không lazy loading)
var collection = await _context.Collections
    .Include(c => c.Assets)
    .Include(c => c.Children)
    .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, ct);

// ✅ Map sang DTO trước khi trả về
return assets.Select(a => a.ToDto()).ToList();

// ❌ Không return IQueryable từ service
// ❌ Không dùng Find() mà không kiểm tra ownership
```

---

## §7 — Migrations Hiện Tại (5)

| Migration | Ngày | Mô tả |
|-----------|------|-------|
| `InitialCreate` | 2026-02-25 | Base schema: Assets, Collections, Identity |
| `AddThumbnailColumns` | 2026-02-26 | ThumbnailSm/Md/Lg trên Assets |
| `AddTagSystem` | 2026-02-27 | Tags + AssetTags M:N |
| `AddCollectionPermissions` | 2026-02-27 | CollectionPermission table |
| `SyncModelChanges` | 2026-02-27 | Model cleanup alignment |

---

> **Document End**
