# 2026-03-20 — Asset Validator & DI registration: refactor

Summary
- Refactor DI registrations and validator wiring: replace previous static-forwarding validator with a concrete `AssetValidatorImpl` and make `AssetMapper` scoped and dependent on the scoped `IAssetFactory`.
- Make the static `AssetValidator` helper `internal` and route public usage through `DefaultAssetValidator.Instance` which now delegates to `AssetValidatorImpl`.

Purpose
- Mục đích: Refactor nhằm cải thiện khả năng tiêm (DI), rõ ràng về lifetime của các service, và tách phần hiện thực validator ra thành một lớp có thể mock/test được. Không phải là thay đổi tính năng (no DB or API contract changes).

Scope (Phạm vi ảnh hưởng)
- Files/Components thay đổi:
  - `VAH.Backend/Extensions/ServiceCollectionExtensions.cs` — DI registrations (validator + mapper lifetimes).
  - `VAH.Backend/Models/AssetFactory.cs` — static facade backing instance adjusted to construct `AssetFactoryImpl` with a concrete `AssetValidatorImpl`.
  - `VAH.Backend/Models/AssetValidator.cs` — changed visibility from `public` → `internal`.
  - `VAH.Backend/Models/DefaultAssetValidator.cs` — now wraps `AssetValidatorImpl` and exposes same API via `DefaultAssetValidator.Instance`.
  - `VAH.Backend/Models/ValueObjects/ColorCode.cs`, `FileName.cs` — now call `DefaultAssetValidator.Instance` instead of `AssetValidator` static.
  - `VAH.Backend/Services/AssetMapper.cs` — now depends on `IAssetFactory` and is registered as Scoped; CreateFileFromDto uses injected factory.

Before & After (So sánh)

Before
- Validator wiring: DI registered a singleton wrapper that forwarded to a static `AssetValidator` helper. `AssetMapper` was registered as singleton and used the static `AssetFactory` facade.

Snippet (before) — DI registration
```csharp
// Asset validator: default wrapper that delegates to the static AssetValidator helper.
// Registered as singleton because it's stateless and forwards to the static helper.
services.AddSingleton<IAssetValidator>(_ => DefaultAssetValidator.Instance);
// Asset mapper: injectable mapping service used across services.
services.AddSingleton<IAssetMapper, AssetMapper>();
```

Snippet (before) — Static AssetFactory backing impl
```csharp
private static readonly IAssetFactory _impl = new AssetFactoryImpl(DefaultAssetValidator.Instance);
```

Behavioral notes (before)
- Static `AssetValidator` (public) was the true location of validation logic; DI layer exposed only a thin singleton wrapper around it. This kept a static API surface and allowed callers to still call static helpers.

After
- Validator: `AssetValidatorImpl` is the concrete implementation used by DI and by `DefaultAssetValidator` (which now internally delegates to `AssetValidatorImpl`). `AssetValidator` helper class was changed to `internal` to restrict direct external use; public callers should use `DefaultAssetValidator.Instance`.
- DI lifetimes: `IAssetValidator` registered as singleton concrete `AssetValidatorImpl`; `IAssetMapper` changed from singleton → scoped because it depends on the scoped `IAssetFactory`.
- `AssetFactory` static backing instance was changed to construct `AssetFactoryImpl(new AssetValidatorImpl())` (note: this bypasses DI container and creates a concrete validator inline).

Snippet (after) — DI registration
```csharp
// Asset validator: concrete implementation registered as singleton.
services.AddSingleton<IAssetValidator, AssetValidatorImpl>();
// Register as Scoped because it depends on the scoped `IAssetFactory`.
services.AddScoped<IAssetMapper, AssetMapper>();
```

Snippet (after) — Static AssetFactory backing impl
```csharp
private static readonly IAssetFactory _impl = new AssetFactoryImpl(new AssetValidatorImpl());
```

Why the new approach is better
- Testability: `AssetValidatorImpl` is a concrete, non-static class that can be mocked or replaced in tests and in alternate DI configurations.
- Lifetime correctness: `AssetMapper` now respects scoped dependencies (`IAssetFactory`) and will not outlive resources it depends on, preventing accidental capture of scoped services by singletons.
- Encapsulation: the `AssetValidator` helper was made `internal` to guide consumers toward the public `DefaultAssetValidator.Instance` API (which itself delegates to `AssetValidatorImpl`).

Trade-offs & Risks
- Static `AssetFactory._impl` still constructs `AssetValidatorImpl` directly with `new` and bypasses the DI container — this is a potential footgun: changes to DI-registered validator implementations will not affect the static facade. Consider refactoring the static facade to resolve dependencies from an `IServiceProvider` or remove the static facade in favor of DI.
- Changing `AssetValidator` from `public` → `internal` is a breaking API for any external assembly that previously referenced it directly; ensure consumers use `DefaultAssetValidator.Instance` or `IAssetValidator` via DI.
- AssetMapper lifetime change must be validated in places where `IAssetMapper` was previously assumed to be singleton (e.g., cached references in long-lived services). Review usages to ensure no singleton holds onto a scoped `IAssetMapper` reference.

Notes for the Team
- DB schema: No DB schema changes.
- API contract: No REST API surface changes.
- Environment variables: No changes.

Recommended Follow-ups
- Replace `AssetFactory` static facade usage in application code with an injectable `IAssetFactory` resolved from DI (or make the static facade resolve from a service provider) to keep a single source of truth for the validator implementation.
- Run a quick scan for any code that depends on the previous `public static class AssetValidator` (outside the assembly) and update to `DefaultAssetValidator.Instance` or `IAssetValidator` via DI.
- Add unit tests for `DefaultAssetValidator` to ensure delegation to `AssetValidatorImpl` keeps behavior identical. Add tests that verify `AssetMapper` receives a scoped `IAssetFactory` and does not rely on static `AssetFactory`.
- Verify runtime behavior for background/singleton services that may have captured `IAssetMapper` previously. If such capture exists, refactor to resolve scoped services per operation.

Dữ liệu đầu vào: git diff

```diff
<the user-provided diff is included in the PR body or referenced here>
```
