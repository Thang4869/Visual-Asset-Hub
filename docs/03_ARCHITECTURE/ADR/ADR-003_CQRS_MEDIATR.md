# ADR-003: CQRS with MediatR for Asset Module

> **Status**: Accepted
> **Date**: 2026-02-27
> **Deciders**: Tech Lead

## Context

The Asset module is the most complex domain area (14 service methods, file I/O, thumbnails, notifications, bulk operations). The original implementation used a single `AssetService` with 14+ methods called directly from a monolithic `AssetsController`.

Problems identified:
- Fat controller with mixed read/write concerns
- Difficult to test individual operations in isolation
- No clear boundary between request validation and business logic
- Hard to add cross-cutting concerns (logging, caching) per operation

## Decision

Introduce **CQRS (Command Query Responsibility Segregation)** using **MediatR 14** for the Asset module:

**Command Side:**
```
UploadAssetsCommand → UploadAssetsHandler → IAssetService
UpdateAssetCommand → UpdateAssetHandler → IAssetService
DeleteAssetCommand → DeleteAssetHandler → IAssetService
DuplicateAssetCommand → DuplicateAssetHandler → IAssetDuplicateStrategyFactory
UpdateAssetPositionCommand → UpdateAssetPositionHandler → IAssetService
```

**Query Side:**
```
GetAssetsQuery → GetAssetsHandler → IAssetService
GetAssetByIdQuery → GetAssetByIdHandler → IAssetService
GetAssetsByFolderQuery → GetAssetsByFolderHandler → IAssetService
```

**Controller Split:**
- `AssetsCommandController` (6 write endpoints)
- `AssetsQueryController` (3 read endpoints)

**Facade Layer:**
- `AssetApplicationService` wraps `ISender` + `IUserContextProvider` + `IOptions<AssetOptions>`
- Provides a simplified API for non-CQRS callers

## Consequences

### Positive
- Single Responsibility: each handler does one thing
- Testable: handlers can be unit-tested with mocked `IAssetService`
- Extensible: MediatR pipeline behaviors for validation, logging, caching
- Clear read/write separation at controller level

### Negative
- More files per operation (command record + handler + potentially validator)
- Indirect dispatch (MediatR) adds a layer of abstraction
- Handlers currently delegate to `IAssetService` — not fully self-contained yet

### Neutral
- Other modules (Collection, Tag, etc.) remain in direct service call pattern until complexity warrants CQRS extraction
- MediatR registered via `RegisterServicesFromAssemblyContaining<AssetService>()`

## Compliance

- New Asset operations MUST be implemented as Command/Query + Handler
- Handlers MUST NOT access `HttpContext` directly — use `IUserContextProvider`
- Commands are `sealed record` types; Queries are `sealed record` types
- Non-asset modules should NOT adopt CQRS unless justified by complexity

---

> **Document End**
