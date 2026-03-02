# ADR-002: TPH Inheritance for Asset Types

> **Status**: Accepted
> **Date**: 2026-02-25
> **Deciders**: Tech Lead

## Context

The system manages 6 distinct asset types (Image, Link, Color, ColorGroup, Folder, File) that share 90% of their schema but differ in behavior:

| Behavior | Image | Link | Color | ColorGroup | Folder | File |
|----------|-------|------|-------|------------|--------|------|
| HasPhysicalFile | ✅ | ❌ | ❌ | ❌ | ❌ | ✅ |
| CanHaveThumbnails | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| RequiresFileCleanup | ✅ | ❌ | ❌ | ❌ | ❌ | ✅ |

Alternatives considered:
1. **TPT (Table-Per-Type)**: Separate table per type → JOINs on every query, complex migrations
2. **TPC (Table-Per-Concrete)**: Full schema per type → data duplication, no polymorphic queries
3. **TPH (Table-Per-Hierarchy)**: Single table, discriminator column → nullable columns but fast queries

## Decision

Use **TPH (Table-Per-Hierarchy)** with `ContentType` as the discriminator column:

```csharp
// Base class — all shared properties
public class Asset { ... }

// Subtypes override virtual behavior properties
public class ImageAsset : Asset
{
    public override bool HasPhysicalFile => true;
    public override bool CanHaveThumbnails => true;
}
public class LinkAsset : Asset
{
    public override bool HasPhysicalFile => false;
}
// ... ColorAsset, ColorGroupAsset, FolderAsset
```

**Discriminator mapping** uses the existing `ContentType` string column (no additional migration):
```
"image"       → ImageAsset
"link"        → LinkAsset
"color"       → ColorAsset
"color-group" → ColorGroupAsset
"folder"      → FolderAsset
"file"        → Asset (base fallback)
```

`AssetFactory` encapsulates construction so callers never use `new ImageAsset()` directly.

## Consequences

### Positive
- Single table = fast polymorphic queries (`SELECT * FROM Assets WHERE CollectionId = @id`)
- No JOINs needed for listing mixed asset types
- Virtual method dispatch replaces switch/if chains (Open/Closed Principle)
- Factory pattern ensures correct type instantiation

### Negative
- Nullable columns for type-specific data (e.g., ThumbnailSm is null for non-image types)
- All types share one large table — index strategy must account for varying column usage
- Adding a new asset type requires updating Factory, EF configuration, and Enum mappings

### Neutral
- EF Core 9 TPH performs a discriminator-based SQL fix on startup to correct any orphaned rows (see `Program.cs` auto-migration)

## Compliance

- New asset types MUST be created as a subclass of `Asset` with correct virtual overrides
- New asset types MUST be added to `AssetFactory.CreateForType()` and `EnumMappings`
- Direct `new Asset()` construction is PROHIBITED in application code — use `AssetFactory`

---

> **Document End**
