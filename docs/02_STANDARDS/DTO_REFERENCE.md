# Tham Chiếu DTO (DTO Reference — Data Transfer Objects)

> **Mục đích**: Tài liệu tham chiếu tất cả Data Transfer Objects trong hệ thống  
> **Last Updated**: 2026-04-06  
> **Nguồn**: Migrated từ `PROJECT_DOCUMENTATION.md` §3  
> **Trạng thái**: Living Document — cập nhật khi DTOs thay đổi

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

## §4 — DTOs Thao Tác Hàng Loạt (Bulk Operation)

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

## §7 — Vị Trí File DTO

| File | DTOs Được Định Nghĩa |
|------|----------------------|
| `Models/DTOs.cs` | CreateFolderDto, CreateColorDto, UpdateAssetDto, CreateLinkDto, CreateColorGroupDto, ReorderAssetsDto, AssetPositionDto, BulkDeleteDto, BulkMoveDto, BulkMoveGroupDto, BulkTagDto, GrantPermissionDto, UpdatePermissionDto, PermissionInfoDto |
| `Models/AuthDTOs.cs` | RegisterDto, LoginDto, AuthResponseDto, AuthValidationConstants |
| `Models/Common.cs` | PagedResult\<T\>, PaginationParams, FileUploadConfig, SmartCollectionDefinition |
| `Models/Tag.cs` | CreateTagDto, UpdateTagDto, AssetTagsDto (inner records) |

---

> **Document End**  
> Liên quan: [API_CONVENTIONS.md](API_CONVENTIONS.md) · [DOMAIN_MODEL.md](../03_ARCHITECTURE/DOMAIN_MODEL.md)
