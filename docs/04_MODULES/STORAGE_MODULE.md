# Storage Module

> **Mục đích**: Upload file, xóa, tạo thumbnail
> **Last Updated**: 2026-04-06

---

## §1 — Tổng quan (Overview)

| Khía cạnh | Chi tiết |
|-----------|----------|
| **Domain** | Upload file, xóa, tạo thumbnail |
| **Services** | `IStorageService` → `LocalStorageService`, `IThumbnailService` → `ThumbnailService` |
| **Library** | SixLabors.ImageSharp 3.1.12 |
| **Storage** | Local filesystem (`wwwroot/uploads/`) |
| **Patterns** | Strategy (IStorageService), Template Method (thumbnail sizes) |

## §2 — Giao diện Service (Service Interfaces)

### IStorageService

```csharp
public interface IStorageService
{
    Task<string> UploadAsync(Stream fileStream, string originalFileName, string contentType);
    Task<bool> DeleteAsync(string filePath);
    string GetPublicUrl(string filePath);
    bool Exists(string filePath);
}
```

Triển khai hiện tại: `LocalStorageService` (dựa trên filesystem).
Được thiết kế để swap sang S3 hoặc Azure Blob qua DI trong tương lai mà không cần thay đổi caller.

### IThumbnailService

```csharp
public interface IThumbnailService
{
    Task<ThumbnailResult?> GenerateAsync(string sourceFilePath, CancellationToken ct);
}
```

## §3 — Bố cục lưu trữ file (File Storage Layout)

```
wwwroot/
└── uploads/
    ├── {uuid}.{ext}              # File gốc (đổi tên UUID)
    └── thumbs/
        ├── sm_{uuid}.webp        # 150px max dimension
        ├── md_{uuid}.webp        # 400px max dimension
        └── lg_{uuid}.webp        # 800px max dimension
```

## §4 — Luồng Upload (Upload Flow)

```
Client              Controller    IAssetService    IStorageService    IThumbnailService
  │                     │              │                    │                   │
  │── POST /upload ────→│              │                    │                   │
  │   (multipart form)  │              │                    │                   │
  │                     │── Upload ───→│                    │                   │
  │                     │              │────── UploadAsync─→│                   │
  │                     │              │   (stream)         │── SaveToDisk      │
  │                     │              │←── /uploads/x ─────│                   │
  │                     │              │── [if image] ─────────────────────────→│
  │                     │              │                    │   GenerateAsync   │
  │                     │              │                    │   (sm, md, lg)    │
  │                     │              │←── ThumbnailResult ────────────────────│
  │                     │              │── SetThumbnails()  │                   │
  │                     │              │── SaveChanges      │                   │
  │                     │←── AssetDto──│                    │                   │
  │←── 201 Created ─────│              │                    │                   │
```

## §5 — Tạo Thumbnail (Thumbnail Generation)

| Kích thước | Kích thước tối đa | Định dạng | Đặt tên |
|------------|------------------|-----------|---------|
| Small | 150px | WebP | `sm_{uuid}.webp` |
| Medium | 400px | WebP | `md_{uuid}.webp` |
| Large | 800px | WebP | `lg_{uuid}.webp` |

- Chỉ được tạo cho loại `ImageAsset` (`CanHaveThumbnails == true`)
- Sử dụng **ImageSharp** để xử lý hình ảnh đa nền tảng
- Duy trì tỷ lệ khung hình (ràng buộc kích thước tối đa)
- Định dạng WebP để nén tối ưu

## §6 — Ràng buộc Upload File (File Upload Constraints)

| Ràng buộc | Giá trị | Nguồn |
|-----------|---------|-------|
| Kích thước file tối đa | 50 MB | `FileUploadConfig` |
| Số file tối đa mỗi request | 20 | `FileUploadConfig` |
| Giới hạn Kestrel body | 100 MB | `Program.cs` |
| Extension cho phép | .jpg, .png, .gif, .webp, .svg, .pdf, .doc, .mp4, .mp3, etc. | `FileUploadConfig` |
| Rate limit | 20/min | Upload rate limiter |

## §7 — Chiến lược dọn dẹp (Cleanup Strategy)

Xóa asset sẽ kích hoạt dọn dẹp file vật lý qua `AssetCleanupHelper`:

```csharp
// Chỉ nếu RequiresFileCleanup (virtual property, true cho Image/File types)
if (asset.RequiresFileCleanup)
{
    await storageService.DeleteAsync(asset.FilePath);
    // Xóa thumbnail nếu có
    if (asset.ThumbnailSm != null) await storageService.DeleteAsync(asset.ThumbnailSm);
    if (asset.ThumbnailMd != null) await storageService.DeleteAsync(asset.ThumbnailMd);
    if (asset.ThumbnailLg != null) await storageService.DeleteAsync(asset.ThumbnailLg);
}
```

---

> **Document End**
