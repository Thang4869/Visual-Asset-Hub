# 2026-03-17 — ApplicationUser: limit public mutability and add audit

Summary

- Refactored the `ApplicationUser` entity to limit public mutability of profile fields.
- Added a domain method `SetDisplayName(string)` to update the display name with validation and audit.
- Added `UpdatedAt` property to `ApplicationUser` and created EF Core migration `AddApplicationUserUpdatedAt`.
- Updated `AuthService` to use the new `ApplicationUser(string displayName)` ctor instead of setting private-set properties.

Files changed

- `VAH.Backend/Models/ApplicationUser.cs` — made `DisplayName` and `CreatedAt` private-set, added ctor, `SetDisplayName`, and `UpdatedAt`.
- `VAH.Backend/Services/AuthService.cs` — construct `ApplicationUser` via `new ApplicationUser(dto.DisplayName)`.
- `VAH.Backend/Migrations/*` — new migration `AddApplicationUserUpdatedAt` generated.

Rationale

- Prevents arbitrary external mutation of user entity properties; mutations must go through domain methods that enforce invariants and add audit timestamps.
- Improves encapsulation and testability.

Backward compatibility & DB impact

- A new nullable column `UpdatedAt` was introduced for `ApplicationUser` via EF migration `AddApplicationUserUpdatedAt`.
- The migration file is generated under `VAH.Backend/Migrations`.
- Applying the migration is required if you want the DB to persist audit information.

How to apply the migration (from repo root)

```powershell
# Create migration (already created):
# dotnet ef migrations add AddApplicationUserUpdatedAt -o VAH.Backend/Migrations --project VAH.Backend/VAH.Backend.csproj --startup-project VAH.Backend/VAH.Backend.csproj

# Apply migration to DB:
dotnet ef database update --project VAH.Backend/VAH.Backend.csproj --startup-project VAH.Backend/VAH.Backend.csproj
```

If you cannot run migrations from this environment, generate the SQL script and have DB admin apply it:

```powershell
dotnet ef migrations script AddApplicationUserUpdatedAt --project VAH.Backend/VAH.Backend.csproj --startup-project VAH.Backend/VAH.Backend.csproj -o AddApplicationUserUpdatedAt.sql
```

Testing & follow-ups

- Add unit tests for `ApplicationUser.SetDisplayName` covering validation, no-op when same value, and `UpdatedAt` set.
- Add integration tests or manual verification to ensure `AuthService.RegisterAsync` still creates users correctly.
- Consider changing other entities with public setters to use private setters + domain methods where invariants are important.

Notes

- Build succeeded after the changes; warnings remain unrelated to this refactor (obsolete `Asset.Tags` usage, nullable deref in `AssetService`).
- Migration generation attempted via `dotnet ef`; the host logs show the app started during the command (this is expected when using the project as startup); the migration file was created.

Suggested reviewers

- Backend/Identity owner
- DB maintainer

---

If you want, I can create a GitHub issue from this text (requires repo permissions) or open a PR with the migration and code changes.
