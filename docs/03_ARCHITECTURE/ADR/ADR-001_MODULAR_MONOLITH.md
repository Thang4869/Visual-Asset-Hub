# ADR-001: Modular Monolith Architecture

> **Status**: Accepted
> **Date**: 2026-02-25
> **Deciders**: Tech Lead

## Context

VAH (Visual Asset Hub) is a team tool for organizing digital assets. The initial architecture needed to balance rapid development velocity with long-term maintainability. A microservice architecture would introduce operational overhead (service discovery, distributed tracing, network latency) disproportionate to the project's current scale (6 DB tables, <100 API endpoints, single-team ownership).

## Decision

Adopt a **Modular Monolith** architecture with clear module boundaries:

- Single deployable unit (ASP.NET Core 9)  
- Module separation by namespace/folder: `Features/Assets/`, `Services/`, `Controllers/`
- Shared `AppDbContext` with per-module entity configuration
- Inter-module communication via direct service injection (DI) — no message bus yet
- Vertical slice extraction starting with the **Asset** module (CQRS + MediatR)

The Asset module serves as the reference implementation for future module extractions:
```
Features/Assets/
├── Application/     → IAssetApplicationService (Facade)
├── Commands/        → MediatR command records + handlers
├── Queries/         → MediatR query records + handlers
├── Common/          → Route constants
├── Contracts/       → DTOs specific to this feature
└── Infrastructure/  → FileMapperService, UserContextProvider
```

Other modules (Collection, Tag, Auth, etc.) remain in the `Services/` layer and will be migrated to the `Features/` pattern incrementally.

## Consequences

### Positive
- Fast development: no network boundaries, shared transaction scope
- Easy debugging: single process, standard debugger
- Clear extraction path: when a module needs independent scaling, the vertical slice boundary is already defined
- Reduced infrastructure cost: 1 container instead of 5+

### Negative
- Shared DB context means modules can bypass boundaries via direct table access
- No independent deployability per module
- Risk of coupling if service interfaces are not strictly enforced

### Neutral
- Docker Compose orchestrates 4 services (Frontend, Backend, PostgreSQL, Redis) — this remains the same whether monolith or microservice

## Compliance

- Each new feature must be evaluated for placement: `Features/` (if asset-related or complex) or `Services/` (if simple CRUD)
- Code reviews must verify no direct `AppDbContext` access from controllers — all access via service interfaces
- Module boundaries documented in `04_MODULES/` for each domain area

---

> **Document End**
