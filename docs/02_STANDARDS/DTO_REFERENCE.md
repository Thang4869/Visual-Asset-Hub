# DTO REFERENCE — Data Transfer Objects

> **Last Updated**: 2026-03-03  
> **Source**: Migrated from `PROJECT_DOCUMENTATION.md` §3  
> **Status**: Living Document — update when DTOs change

---

## §1 — Auth DTOs

| DTO | Fields |
|-----|--------|
| `RegisterDto` | DisplayName (Required, MaxLength 100), Email (Required, EmailAddress, MaxLength 256), Password (Required, MinLength 8, MaxLength 100, Regex policy), ConfirmPassword (Required, Compare with Password) |
| `LoginDto` | Email (Required, EmailAddress), Password (Required) |
| `AuthResponseDto` | Token, Expiration, RefreshToken, RefreshTokenExpiration, UserId, Email, DisplayName |

---

## §2 — Asset DTOs

| DTO | Fields |
|-----|--------|
| `CreateFolderDto` | FolderName (Required), CollectionId, ParentFolderId? |
| `CreateColorDto` | ColorCode (Required), ColorName?, CollectionId, GroupId?, SortOrder?, ParentFolderId? |
| `UpdateAssetDto` | FileName?, SortOrder?, GroupId?, ParentFolderId?, ClearParentFolder? |
| `CreateLinkDto` | Name (Required), Url (Required), CollectionId, ParentFolderId? |
| `CreateColorGroupDto` | GroupName (Required), CollectionId, ParentFolderId?, SortOrder? |
| `ReorderAssetsDto` | AssetIds (List\<int\>, Required) |
| `AssetPositionDto` | PositionX, PositionY |

---

## §3 — Tag DTOs

| DTO | Fields |
|-----|--------|
| `CreateTagDto` | Name (Required), Color? |
| `UpdateTagDto` | Name?, Color? |
| `AssetTagsDto` | TagIds (List\<int\>, Required) |

---

## §4 — Bulk Operation DTOs

| DTO | Fields |
|-----|--------|
| `BulkDeleteDto` | AssetIds (List\<int\>, Required) |
| `BulkMoveDto` | AssetIds (Required), TargetCollectionId?, TargetFolderId?, ClearParentFolder? |
| `BulkMoveGroupDto` | AssetIds (List\<int\>, Required), TargetGroupId (int?), InsertBeforeId (int?) |
| `BulkTagDto` | AssetIds (Required), TagIds (Required), Remove (bool, default false) |

---

## §5 — Permission DTOs

| DTO | Fields |
|-----|--------|
| `GrantPermissionDto` | UserEmail (Required), Role (Required, MaxLength 20) |
| `UpdatePermissionDto` | Role (Required, MaxLength 20) |
| `PermissionInfoDto` | Id, UserId, UserEmail?, DisplayName?, Role, GrantedAt |

---

## §6 — Common DTOs

| DTO | Fields |
|-----|--------|
| `PagedResult<T>` | Items, TotalCount, Page, PageSize, HasNextPage, HasPreviousPage, TotalPages |
| `PaginationParams` | Page (default 1), PageSize (default 50, max 100), SortBy?, SortOrder (default "asc") |
| `FileUploadConfig` | MaxFileSizeBytes (50MB), MaxFilesPerRequest (20), AllowedExtensions (27), AllowedMimeTypePrefixes (13) |
| `SmartCollectionDefinition` | Id, Name, Description, Icon, Color, Count |

---

## §7 — DTO Location Map

| File | DTOs Defined |
|------|-------------|
| `Models/DTOs.cs` | CreateFolderDto, CreateColorDto, UpdateAssetDto, CreateLinkDto, CreateColorGroupDto, ReorderAssetsDto, AssetPositionDto, BulkDeleteDto, BulkMoveDto, BulkMoveGroupDto, BulkTagDto, GrantPermissionDto, UpdatePermissionDto, PermissionInfoDto |
| `Models/AuthDTOs.cs` | RegisterDto, LoginDto, AuthResponseDto, AuthValidationConstants |
| `Models/Common.cs` | PagedResult\<T\>, PaginationParams, FileUploadConfig, SmartCollectionDefinition |
| `Models/Tag.cs` | CreateTagDto, UpdateTagDto, AssetTagsDto (inner records) |

---

> **Document End**  
> Related: [API_CONVENTIONS.md](API_CONVENTIONS.md) · [DOMAIN_MODEL.md](../03_ARCHITECTURE/DOMAIN_MODEL.md)
