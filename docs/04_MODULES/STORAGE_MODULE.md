# STORAGE MODULE

> **Last Updated**: 2026-03-02
> **Status**: Active — Services/ layer

---

## §1 — Overview

| Aspect | Detail |
|--------|--------|
| **Domain** | File upload, deletion, thumbnail generation |
| **Services** | `IStorageService` → `LocalStorageService`, `IThumbnailService` → `ThumbnailService` |
| **Library** | SixLabors.ImageSharp 3.1.12 |
| **Storage** | Local filesystem (`wwwroot/uploads/`) |
| **Patterns** | Strategy (IStorageService), Template Method (thumbnail sizes) |

## §2 — Service Interfaces

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

Current implementation: `LocalStorageService` (filesystem-based).
Designed for future swap to S3 or Azure Blob via DI without changing callers.

### IThumbnailService

```csharp
public interface IThumbnailService
{
    Task<ThumbnailResult?> GenerateAsync(string sourceFilePath, CancellationToken ct);
}
```

## §3 — File Storage Layout

```
wwwroot/
└── uploads/
    ├── {uuid}.{ext}              # Original file (UUID-renamed)
    └── thumbs/
        ├── sm_{uuid}.webp        # 150px max dimension
        ├── md_{uuid}.webp        # 400px max dimension
        └── lg_{uuid}.webp        # 800px max dimension
```

## §4 — Upload Flow

```
Client              Controller    IAssetService    IStorageService    IThumbnailService
  │                     │              │                │                   │
  │── POST /upload ────→│              │                │                   │
  │   (multipart form)  │              │                │                   │
  │                     │── Upload ───→│                │                   │
  │                     │              │── UploadAsync──→│                   │
  │                     │              │   (stream)      │── SaveToDisk     │
  │                     │              │←── /uploads/x  │                   │
  │                     │              │── [if image] ──────────────────────→│
  │                     │              │                │   GenerateAsync    │
  │                     │              │                │   (sm, md, lg)     │
  │                     │              │←── ThumbnailResult ────────────────│
  │                     │              │── SetThumbnails()                  │
  │                     │              │── SaveChanges                     │
  │                     │←── AssetDto──│                │                   │
  │←── 201 Created ─────│              │                │                   │
```

## §5 — Thumbnail Generation

| Size | Max Dimension | Format | Naming |
|------|--------------|--------|--------|
| Small | 150px | WebP | `sm_{uuid}.webp` |
| Medium | 400px | WebP | `md_{uuid}.webp` |
| Large | 800px | WebP | `lg_{uuid}.webp` |

- Only generated for `ImageAsset` types (`CanHaveThumbnails == true`)
- Uses **ImageSharp** for cross-platform image processing
- Maintains aspect ratio (max dimension constraint)
- WebP format for optimal compression

## §6 — File Upload Constraints

| Constraint | Value | Source |
|-----------|-------|--------|
| Max file size | 50 MB | `FileUploadConfig` |
| Max files per request | 20 | `FileUploadConfig` |
| Kestrel body limit | 100 MB | `Program.cs` |
| Allowed extensions | .jpg, .png, .gif, .webp, .svg, .pdf, .doc, .mp4, .mp3, etc. | `FileUploadConfig` |
| Rate limit | 20/min | Upload rate limiter |

## §7 — Cleanup Strategy

Asset deletion triggers physical file cleanup via `AssetCleanupHelper`:

```csharp
// Only if RequiresFileCleanup (virtual property, true for Image/File types)
if (asset.RequiresFileCleanup)
{
    await storageService.DeleteAsync(asset.FilePath);
    // Delete thumbnails if present
    if (asset.ThumbnailSm != null) await storageService.DeleteAsync(asset.ThumbnailSm);
    if (asset.ThumbnailMd != null) await storageService.DeleteAsync(asset.ThumbnailMd);
    if (asset.ThumbnailLg != null) await storageService.DeleteAsync(asset.ThumbnailLg);
}
```

---

> **Document End**
