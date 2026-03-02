# Visual Asset Hub — Architecture Assessment

> **Date:** 02/03/2026 | **Version:** 10.0  
> **Author:** Architecture Review  
> **Status:** Living Document — review quarterly or before major releases  
> **Audience:** Technical leadership, development team, future contributors, investors (due diligence)

> **📖 Reading Guide:** This document uses **collapsible sections** (`▶ Click to expand`) to keep the top-level flow scannable (~600 lines visible). Deep-dive tables, matrices, and analysis are hidden behind expandable toggles — click any `<details>` summary to reveal. Total content: ~1,600 lines across 29 sections.

---

## Executive Summary

**Visual Asset Hub (VAH)** là nền tảng quản lý digital assets (ảnh, link, bảng màu) cho individual creators và small teams. Hệ thống đã hoàn thành 4 giai đoạn phát triển (26/26 items) cùng OOP refactor, đạt được tập feature phong phú bao gồm: RBAC sharing, real-time sync, bulk operations, smart collections và tag system.

### Architecture Maturity Assessment

| Dimension | Level | Verdict |
|-----------|-------|---------|
| **Architecture Style** | Modular Monolith + Service Layer | Phù hợp cho team size và scope hiện tại |
| **Domain Modeling** | Rich Domain Model (TPH inheritance, domain methods) | Tốt — vượt trên Anemic Domain Model |
| **Separation of Concerns** | Partial CQRS (Assets only), no repository layer | Đủ dùng, nhưng sẽ cản scale nếu không cải tiến |
| **Testing** | **Zero coverage** | **Rủi ro cao nhất của dự án** |
| **DevOps** | Docker Compose, no CI/CD | Chỉ phù hợp single-instance deployment |
| **Scalability** | Single-instance only (local storage, SQLite dev) | Chưa sẵn sàng cho horizontal scaling |

### Strategic Verdict

Kiến trúc hiện tại **đủ tốt cho MVP và small-scale production** (≤50 concurrent users, single instance). Tuy nhiên, nếu mục tiêu 6–12 tháng bao gồm team growth, multi-instance deployment, hoặc enterprise adoption, cần giải quyết **3 architectural debt lớn**: zero test coverage, tight EF Core coupling, và single-instance storage constraint.

---

## 1. Architectural Principles

Core design philosophy governing technical decisions. These principles are **prescriptive** — new code must align, existing code should converge.

| # | Principle | Implication | Current Compliance |
|---|-----------|-------------|--------------------|
| P1 | **User Data Isolation First** | Every entity scoped by UserId. No cross-tenant data leakage, even in error paths. | ✅ Enforced — FK + service layer |
| P2 | **Interface Over Implementation** | All services behind interfaces. Storage, cache, notification — swappable without controller changes. | ✅ 12/12 services |
| P3 | **Fail Loud in Dev, Fail Safe in Prod** | Full exception details in Development; ProblemDetails (RFC 7807) only in Production. Auto-migrate in dev; explicit migration in prod. | 🟡 Auto-migrate not yet gated |
| P4 | **Optimize for Read Path** | Reads vastly exceed writes. Cache collection lists, pre-generate thumbnails, index aggressively. Write path can tolerate higher latency. | ✅ Redis cache + 22 indexes |
| P5 | **Progressive Complexity** | Start simple (Context API, local storage, single instance). Add complexity (Zustand, S3, multi-instance) only when measured need arises. | ✅ Current stage appropriate |
| P6 | **Contract-Driven Boundaries** | Frontend↔Backend contract defined by Swagger. SignalR events are typed. Breaking changes require version bump. | 🟡 No contract tests yet |
| P7 | **Infrastructure as Code** | All deployment config in docker-compose.yml / Dockerfiles. No manual server configuration. | ✅ Docker multi-stage |

<details>
<summary><strong>Key Trade-offs Accepted</strong> (4 decisions)</summary>

| Trade-off | Chosen | Alternative | Why |
|-----------|--------|-------------|-----|
| EF Core direct in services vs. Repository | Direct DbContext | Generic Repository | Team of 1–3, <10 entities. Repository adds abstraction tax without proportional benefit at this scale. Re-evaluate at 15+ entities. |
| Context API vs. State library | Context API | Zustand / Redux | Zero dependency cost. Acceptable until >20 components or measured re-render issues. |
| Monolith vs. Microservices | Modular Monolith | Service mesh | Single deployable unit = simpler ops. Feature slices provide internal modularity without network overhead. |
| SQLite dev vs. Unified PostgreSQL | Dual provider | PostgreSQL everywhere | Developer convenience (no Docker required for dev). Mitigated by `DatabaseProviderInfo` dialect handling. |

</details>

---

## 2. Architectural Constraints (Deliberate Non-Support Declarations)

These are intentional boundaries — what VAH will **NOT** support. Each eliminates an entire class of complexity. Violating a constraint without an ADR is an architectural breach.

| # | Constraint | Rationale | Re-evaluation Trigger |
|---|-----------|-----------|----------------------|
| C1 | **No multi-tenancy** | Single-user or team-scoped data plane. Shared infra, isolated data. Adding tenant isolation multiplies every query, cache key, and auth check. | Enterprise contract requiring hard tenant isolation |
| C2 | **No offline-first / PWA** | Always-connected assumption. Local-first sync engines (CRDTs, conflict resolution) are an order of magnitude more complex than our entire backend. | Mobile app requirement with unreliable connectivity |
| C3 | **No microservices before 3 developers** | Monolith is correct for team of 1. Distributed systems tax (network latency, partial failure, eventual consistency, distributed tracing) exceeds benefit. | PT5 threshold in §24 |
| C4 | **No custom auth provider / OAuth federation** | ASP.NET Identity + JWT only. No SAML, no OIDC, no social login. Each provider adds attack surface and maintenance burden. | Enterprise SSO requirement |
| C5 | **No real-time collaboration** | SignalR for push notifications only, not operational transform / CRDT co-editing. Real-time collaboration is a product unto itself. | Google Docs-style co-editing requirement |
| C6 | **No mobile-native app** | Web-only (responsive SPA). React Native / Flutter doubles the frontend surface area with no backend benefit. | >30% mobile traffic with poor responsive UX |
| C7 | **No file versioning / history** | Upload overwrites. No Git-like history for assets. Versioning triples storage cost and requires a diff engine. | Regulatory compliance requiring audit trail |
| C8 | **No multi-region deployment** | Single region. No geo-replication. Multi-region adds eventual consistency, conflict resolution, and 3× infra cost. | Latency >200ms to primary region for majority of users |
| C9 | **No GraphQL** | REST-only API surface. GraphQL adds schema complexity, N+1 resolver traps, and caching difficulty without proportional benefit at <15 entities. | Frontend team requesting flexible queries across >10 joined entities |
| C10 | **Max 50MB per file, no chunked upload** | Hard limit. Chunked/resumable upload (tus protocol) is a significant engineering investment. | User demand for video assets or large PSD files |

<details>
<summary><strong>Constraint Violation Process</strong></summary>

1. **Identify:** Developer recognizes a feature requires violating a constraint
2. **ADR:** Write ADR documenting why the constraint no longer holds
3. **Review:** Principal engineer or tech lead approves
4. **Update:** This section updated — constraint moved to "Retired" with date and ADR reference
5. **Implement:** Proceed with implementation

> **Principle:** Saying NO to things is as important as saying YES. These constraints protect the team from accidental complexity that kills startups.

</details>

---

## 3. Architecture Governance Model

<details>
<summary><strong>Governance Details</strong> — ADR format, Breaking Changes, Review Cadence, Health Rules GR1–GR10</summary>

### Decision Authority

| Decision Type | Authority | Process | Record |
|---------------|-----------|---------|--------|
| Tactical (library update, config change) | Any developer | PR review | Commit message |
| Standard (new service, new entity, API change) | Tech lead | PR + design discussion | ADR in `docs/adr/` |
| Strategic (new domain, infra change, breaking API) | Principal + stakeholder | RFC → review → approve | ADR + this document update |

### Architecture Decision Records (ADRs)

**Location:** `docs/adr/NNN-title.md` (to be created)

**Format:**
```
# ADR-NNN: [Title]
Date: YYYY-MM-DD
Status: Proposed | Accepted | Deprecated | Superseded by ADR-XXX
Context: [Why this decision is needed]
Decision: [What was decided]
Consequences: [Trade-offs accepted]
```

**When to write an ADR:**
- Adding or removing a major dependency
- Changing data model relationships
- Introducing a new architectural pattern
- Modifying API contracts with breaking changes
- Changing deployment topology

### Breaking Change Policy

| Scope | Policy | Notice Period |
|-------|--------|---------------|
| API endpoint removal | Deprecate → alias → remove after 2 releases | 2 sprints minimum |
| API response shape change | Additive only (no field removal) | None for additions |
| SignalR event rename/remove | Coordinate with frontend before merge | Same release cycle |
| Database migration (destructive) | Backup required, staging validation mandatory | 1 sprint |
| Environment variable rename | Update all docker-compose profiles + docs | Same release |

### Review Cadence

| Activity | Frequency | Owner | Output |
|----------|-----------|-------|--------|
| Architecture doc review | Quarterly | Tech lead | Version bump, stale content pruned |
| Dependency audit | Quarterly | Any developer | Dependency Risk Matrix updated (§9) |
| Security posture review | Semi-annually | Tech lead | Threat model refresh (§17) |
| SLO review | Monthly (when monitoring exists) | Ops / Tech lead | SLO targets adjusted |
| Tech debt triage | Per sprint | Team | Debt items prioritized in backlog |

### Codebase Health Governance Rules

Measurable thresholds enforced via CI (where tooling exists) and manual audit quarterly (where not automated). These are **hard rules**, not guidelines.

| Rule | Metric | Threshold | Enforcement | Current Status |
|------|--------|-----------|-------------|----------------|
| GR1 | **Max file LOC** (any single .cs/.jsx/.js) | ≤300 lines | Lint rule / PR review | ❌ Violated: AppContext 472, App.jsx 477, ColorBoard 555 |
| GR2 | **Max cyclomatic complexity** per method | ≤15 | Static analysis (SonarQube / Roslyn analyzer) | Unknown — no tooling |
| GR3 | **Service layer test coverage** | ≥70% | CI gate via coverlet | ❌ 0% |
| GR4 | **No `DbContext` in controllers** | 0 direct usages | Roslyn analyzer or `grep` in CI | ✅ Compliant |
| GR5 | **API response time P95** | <500ms | Runtime monitoring (§21 Observability) | Unknown — no monitoring |
| GR6 | **Max controller action methods** | ≤10 per controller | PR review | ✅ Compliant |
| GR7 | **Zero `TODO` / `HACK` / `FIXME` in main branch** | 0 count | `grep` in CI pre-merge hook | Unknown — not measured |
| GR8 | **Dependency freshness** | No dependency >1 major version behind | Dependabot / manual quarterly audit | 🟡 .NET 9 STS → must plan .NET 10 |
| GR9 | **No destructive migration without rollback script** | 100% compliance | PR review + staging validation | ❌ No staging exists |
| GR10 | **Frontend bundle size** | <500KB gzipped | CI check via `vite-plugin-inspect` or build output | ✅ ~200KB |

#### Violation Process

1. **New violations** → block PR merge (when CI enforced)
2. **Existing violations** → tracked in Tech Debt §7 with repayment timeline
3. **Waiver** → requires ADR documenting why the threshold is inappropriate for this case
4. **Quarterly review** → thresholds adjusted if consistently too strict or too lenient

</details>

---

## 4. Current Architecture

### 4.1 System Topology

```
┌──────────────────────────────────────────────────────┐
│  React 19 SPA (Vite 7)                               │
│  Context API + 11 hooks + 17 components               │
└──────────────┬───────────────────────────────────────┘
               │ HTTP REST + SignalR WebSocket
               ▼
┌──────────────────────────────────────────────────────┐
│  Nginx Reverse Proxy                                  │
│  SPA fallback · Gzip · Cache /assets/ 1yr             │
└──────────────┬───────────────────────────────────────┘
               ▼
┌──────────────────────────────────────────────────────┐
│  ASP.NET Core 9 Backend (port 5027)                   │
│  ┌────────────────────────────────────────────────┐   │
│  │ Middleware Pipeline                             │   │
│  │ Exception → CORS → Serilog → RateLimit →        │   │
│  │ StaticFiles → Auth → Controllers + SignalR      │   │
│  ├────────────────────────────────────────────────┤   │
│  │ Controllers (Feature-sliced for Assets)         │   │
│  │ Command/Query + Layout + Bulk + Auth +          │   │
│  │ Collections + Search + Tags + Smart + Perms     │   │
│  ├────────────────────────────────────────────────┤   │
│  │ Service Layer (12 services, interface-backed)   │   │
│  │ Strategy Pattern (SmartCollection filters)      │   │
│  ├────────────────────────────────────────────────┤   │
│  │ EF Core 9 + ASP.NET Identity                    │   │
│  │ TPH Inheritance (Asset hierarchy)               │   │
│  └────────────────────────────────────────────────┘   │
└─────┬──────────────┬──────────────┬──────────────────┘
      ▼              ▼              ▼
 PostgreSQL 17   Redis 7      Local Storage
 (SQLite dev)    (cache)      wwwroot/uploads
```

### 4.2 Tech Stack

| Layer | Technology | Version |
|-------|-----------|---------|
| Backend Runtime | .NET / ASP.NET Core | 9.0 |
| ORM | Entity Framework Core | 9.x |
| Database | PostgreSQL (prod) / SQLite (dev) | 17 / embedded |
| Cache | Redis + in-memory fallback | 7 Alpine |
| Auth | ASP.NET Identity + JWT Bearer | HS256, 24h TTL |
| Real-time | SignalR | 9.x |
| Image Processing | SixLabors.ImageSharp | 3.1 |
| Logging | Serilog (Console + File) | 10.x |
| Frontend | React 19 + Vite 7 | 19.2 / 7.3 |
| Containerization | Docker + docker-compose | Multi-stage |

<details>
<summary><strong>4.3 Domain Model</strong> — 5 entities, TPH inheritance</summary>

### 4.3 Domain Model

5 entity tables + ASP.NET Identity:

```
AspNetUsers ──┬──< Assets (UserId FK, Cascade)
              ├──< Collections (ParentId self-ref, SetNull)
              ├──< Tags (UserId FK, Cascade)
              └──< CollectionPermissions (UserId FK, Cascade)

Collections ──┬──< Assets (CollectionId FK, Cascade)
              └──< CollectionPermissions (CollectionId FK, Cascade)

Assets ──< AssetTags ──> Tags  (M2M junction, Cascade both)
Assets ──< Assets (ParentFolderId self-ref, Restrict — folders)
```

Asset sử dụng **TPH (Table-Per-Hierarchy) inheritance** với discriminator `ContentType`:
- `ImageAsset`, `LinkAsset`, `ColorAsset`, `ColorGroupAsset`, `FolderAsset`
- Virtual properties (`HasPhysicalFile`, `CanHaveThumbnails`, `RequiresFileCleanup`) cho domain behavior

</details>

<details>
<summary><strong>4.4 Key Architecture Decisions</strong> — 6 ADRs</summary>

### 4.4 Key Architecture Decisions (ADRs)

| # | Decision | Rationale | Trade-off |
|---|----------|-----------|-----------|
| 1 | Feature Slice cho Assets (Command/Query split) | Tách read/write, dễ scale theo vertical | Chỉ áp dụng cho Assets, inconsistent với phần còn lại |
| 2 | Service Layer trực tiếp dùng DbContext (no Repository) | Giảm abstraction overhead cho team nhỏ | Tight coupling với EF Core, khó mock trong unit test |
| 3 | Dual DB Provider (SQLite dev / PostgreSQL prod) | Zero-config dev setup | Behavioral drift giữa 2 providers |
| 4 | Context API thay vì Redux/Zustand | Đơn giản, không thêm dependency | God Context risk, re-render cascade |
| 5 | Local file storage + IStorageService interface | Đơn giản cho MVP, interface sẵn sàng swap | Chặn horizontal scaling |
| 6 | Auto-migrate on startup | Tiện cho dev | Nguy hiểm cho production |

</details>

---

## 5. Domain Boundary & Context Map

Applying Domain-Driven Design (DDD) thinking — even without full DDD implementation — to identify bounded contexts and guide future modularization.

### Core Domain (competitive advantage)

| Context | Responsibility | Key Entities | Owner |
|---------|---------------|--------------|-------|
| **Asset Management** | Upload, store, retrieve, organize digital assets | `Asset`, `ImageAsset`, `LinkAsset`, `ColorAsset`, `FolderAsset` | Backend — `AssetService`, CQRS handlers |
| **Smart Collections** | Rule-based dynamic grouping using Strategy pattern | `SmartCollection`, filter strategies | Backend — `SmartCollectionService` |

### Supporting Domain (necessary but not differentiating)

| Context | Responsibility | Key Entities | Owner |
|---------|---------------|--------------|-------|
| **Organization** | Collections, folders, tags, color groups — structural taxonomy | `Collection`, `Tag`, `Folder`, `ColorGroup` | Backend — respective services |
| **Search** | Full-text and attribute search across assets | Search DTOs, query builders | Backend — `SearchService` |

### Generic Domain (commodity, consider off-the-shelf)

| Context | Responsibility | Key Entities | Status |
|---------|---------------|--------------|--------|
| **Identity & Auth** | User registration, login, JWT token management | `ApplicationUser`, JWT tokens | Implemented (ASP.NET Identity) |
| **Permissions** | Collection-level access control | `CollectionPermission` | Basic — should be a library/middleware |
| **Real-Time Sync** | Push updates to connected clients | SignalR hub events | Implemented (SignalR) |

<details>
<summary><strong>Context Map</strong> — ASCII diagram + coupling table</summary>

### Context Map

```
┌─────────────────────────────────────────────────┐
│           CORE DOMAIN                         │
│  ┌────────────────┐  ┌────────────────────┐  │
│  │ Asset Mgmt     │──│ Smart Collections   │  │
│  │ (CQRS partial) │  │ (Strategy pattern)  │  │
│  └────────┬───────┘  └──────────┬─────────┘  │
└─────────────│─────────────│─────────────────────┘
              │             │
┌─────────────┴─────────────┴─────────────────────┐
│        SUPPORTING DOMAIN                       │
│  ┌────────────┐ ┌────────┐ ┌──────────────┐  │
│  │ Organization │ │ Search │ │ Tags + Colors  │  │
│  └────────────┘ └────────┘ └──────────────┘  │
└─────────────────────────────────────────────────┘
              │
┌─────────────┴───────────────────────────────────┐
│         GENERIC DOMAIN                         │
│  ┌────────────┐ ┌─────────────┐ ┌────────────┐  │
│  │ Identity   │ │ Permissions │ │ Real-Time  │  │
│  │ (ASP.NET)  │ │ (basic ACL) │ │ (SignalR)  │  │
│  └────────────┘ └─────────────┘ └────────────┘  │
└─────────────────────────────────────────────────┘
```

### Coupling Between Contexts

| From | To | Coupling Type | Assessment |
|------|----|---------------|------------|
| Asset Mgmt | Organization | Direct DB FK | Acceptable (same monolith) — decouple if extracting to microservice |
| Asset Mgmt | Identity | `CreatedBy` FK | Acceptable |
| Smart Collections | Asset Mgmt | Strategy reads asset table | Acceptable — shared kernel in monolith |
| Search | Asset Mgmt + Org | Queries across both | Acceptable — extract search index when scaling |
| Real-Time | All | SignalR hub broadcasts on entity changes | Loose coupling (event-based) ✅ |

**Future Guidance:** If VAH evolves beyond single-team monolith, extract **Core Domain** contexts first. Supporting and Generic domains can remain coupled longer.

</details>

---

## 6. Identified Anti-Patterns

### 6.1 God Context (Frontend)

**File:** `AppContext.js` — 472 dòng, compose **tất cả** domain hooks (collections, assets, tags, signalR, undoRedo, smartCollections).

**Hệ quả:** Bất kỳ state change nào (ví dụ: thêm 1 tag) đều trigger re-render toàn bộ component tree qua context. Với 17 components, đây là performance bottleneck tiềm ẩn.

**Khuyến nghị:** Tách thành multiple focused contexts (AssetContext, CollectionContext, TagContext) hoặc migrate sang Zustand với selector pattern.

### 6.2 Fat Component

**File:** `App.jsx` — 477 dòng, destructure ~50 values từ `useAppContext()`.

**Hệ quả:** Khó maintain, khó test, violate Single Responsibility Principle. Mọi feature mới đều phình component này.

**Khuyến nghị:** Extract layout regions thành container components, mỗi component chỉ consume phần state cần thiết.

### 6.3 Service–DbContext Direct Coupling

**Hiện trạng:** Tất cả 12 services inject `AppDbContext` trực tiếp, viết LINQ queries inline.

**Hệ quả:**
- Không thể unit test business logic mà không spin up in-memory database
- Query logic phân tán khắp service layer, khó audit N+1
- Thay đổi schema impact rộng (ripple effect)

**Đánh giá:** Acceptable cho project size hiện tại (<50 entities). Sẽ trở thành nợ kỹ thuật nghiêm trọng nếu domain mở rộng.

### 6.4 Auto-Migrate on Startup

`Database.Migrate()` chạy mỗi khi application start.

**Rủi ro production:** Migration failure = app không start = downtime. Không có rollback path. Không review migration trước khi apply.

**Khuyến nghị:** Tách migration thành explicit step trong CI/CD pipeline.

### 6.5 Manual Data Fetching (Frontend)

Pattern `useEffect` + `useState` cho mọi API call:
- Không cache, không dedup, không background refetch
- Re-fetch toàn bộ data khi navigate
- Không stale-while-revalidate

**Khuyến nghị:** Adopt TanStack Query (React Query) — giải quyết caching, dedup, retry, optimistic updates trong 1 library.

---

## 7. Technical Debt Classification

Debt items from Sections 6 and 8 classified by nature, enabling targeted repayment strategies.

<details>
<summary><strong>Debt Tables</strong> — Tactical (5) + Structural (5) + Operational (5) + Strategic (5)</summary>

### Tactical Debt (quick fixes, low blast radius)

| Item | Origin | Repayment Cost | Risk if Unpaid |
|------|--------|---------------|----------------|
| Auto-migrate on startup | Convenience shortcut | 0.5 day | Production downtime on bad migration |
| Hardcoded `API_URL` in frontend | Initial MVP | 0.5 day | Cannot deploy staging/prod without rebuild |
| JWT key in appsettings | Quick setup | 0.5 day | Credential leak in source control |
| No URL scheme validation | Oversight | 0.5 day | XSS via `javascript:` link assets |
| Swagger disabled in prod | Default config | 0.5 day | API documentation inaccessible to partners |

### Structural Debt (architecture-level, requires refactoring)

| Item | Origin | Repayment Cost | Risk if Unpaid |
|------|--------|---------------|----------------|
| God Context (AppContext 472 LOC) | Organic growth | 2–3 days | Re-render cascade, FE perf degradation |
| Fat Component (App.jsx 477 LOC) | Feature accumulation | 2–3 days | Unmaintainable entry point |
| Service–DbContext coupling | Simplicity trade-off | 5–7 days (if adding repo) | Untestable business logic, N+1 blind spots |
| Manual `useEffect`+`useState` fetching | No data layer adopted | 3–5 days | No caching, no dedup, poor UX on slow networks |
| Partial CQRS (Assets only) | Incremental adoption | 3–5 days per domain | Inconsistent patterns across controllers |

### Operational Debt (deployment, monitoring, reliability)

| Item | Origin | Repayment Cost | Risk if Unpaid |
|------|--------|---------------|----------------|
| Zero test coverage | Velocity-first development | 2–3 days setup + ongoing | Regression risk compounds with every change |
| No CI/CD pipeline | Manual workflow | 1–3 days | Human error on deploy, no automated quality gate |
| No HTTPS default | Dev-first config | 0.5 day | Plaintext credentials in production |
| No automated backups | Not prioritized | 1 day | Unrecoverable data loss |
| No metrics/alerting beyond health check | Not implemented | 2–3 days | Silent failures, no SLO tracking |

### Strategic Debt (blocks future growth trajectories)

| Item | Origin | Repayment Cost | Risk if Unpaid |
|------|--------|---------------|----------------|
| Local-only file storage | MVP scope | 2–3 days | **Blocks horizontal scaling entirely** |
| No concurrency control | Not designed | 1 day | Silent data corruption with concurrent editors |
| LIKE-based search | Simplest implementation | 3–5 days | Performance wall at >10K assets |
| SQLite in dev path | Convenience | 1–2 days to remove | Behavioral drift = prod-only bugs |
| No SignalR backplane | Single instance assumption | 1 day | Blocks multi-instance real-time |

</details>

**Debt Burn-Down Priority:** Tactical → Operational → Structural → Strategic (aligns with Stabilize → Modularize → Scale roadmap).

---

## 8. Architectural Risks

### Risk Matrix

| # | Risk | Severity | Likelihood | Impact | Mitigation Status |
|---|------|----------|------------|--------|-------------------|
| R1 | **Zero test coverage** | 🔴 Critical | Certain | Regression bugs vào production, refactor = gamble | ❌ No mitigation |
| R2 | **Single-instance constraint** (local storage + SQLite dev) | 🔴 High | High nếu scale | No HA, single point of failure | 🟡 Interface ready, chưa implement |
| R3 | **No CI/CD pipeline** | 🟠 High | Certain | Manual deploy = error-prone, no rollback | ❌ No mitigation |
| R4 | **No concurrency control** | 🟠 High | Medium | Last-write-wins → silent data corruption với ≥2 concurrent editors | ❌ No mitigation |
| R5 | **God Context re-render cascade** | 🟡 Medium | Growing | UX degradation khi component count tăng | ❌ No mitigation |
| R6 | **LIKE-based search** | 🟡 Medium | Growing | Query time tăng tuyến tính theo data volume | ❌ No mitigation |
| R7 | **No HTTPS enforcement** | 🟡 Medium | Certain in prod | Credentials + data transmitted plaintext | ❌ No mitigation |
| R8 | **SQLite/PostgreSQL behavioral drift** | 🟡 Medium | Ongoing | Bugs chỉ xuất hiện trên production | 🟡 Partially mitigated (DatabaseProviderInfo) |
| R9 | **XSS via link URLs** | 🟡 Medium | Low | `javascript:` URLs có thể được lưu và render | ❌ No mitigation |
| R10 | **No secret management** | 🟡 Medium | Certain in prod | JWT key + DB credentials trong appsettings | ❌ No mitigation |

<details>
<summary><strong>Risk R1 Deep Dive — Zero Test Coverage</strong></summary>

Đây là rủi ro hệ thống quan trọng nhất. Với 12 backend services, 11 frontend hooks, 17 components và ~40 API endpoints:
- **Mỗi code change** đều có khả năng gây regression không phát hiện được
- **Refactor** (ví dụ: tách God Context) trở nên rủi ro cao vì không có safety net
- **API contract** giữa frontend/backend không được verify tự động

**Business impact:** Development velocity sẽ giảm dần theo thời gian vì team phải manual test mọi thứ, và bug density tăng với codebase size.

</details>

---

## 9. External Dependency Risk Matrix

Every external dependency introduces lifecycle, security, and compatibility risk. This matrix tracks current exposure.

<details>
<summary><strong>Dependency Tables</strong> — 8 runtime deps + 8 libraries + .NET 10 migration checklist</summary>

### Runtime Dependencies

| Dependency | Current Version | EOL / Support Window | Upgrade Risk | Action |
|------------|----------------|---------------------|--------------|--------|
| **.NET** | 9.0 (STS) | May 2026 (6 months of support) | 🟠 Must upgrade to .NET 10 LTS by mid-2026 | Plan .NET 10 upgrade sprint |
| **ASP.NET Core** | 9.0 | Tied to .NET 9 STS | 🟠 Same as above | Same as above |
| **EF Core** | 9.0 | Tied to .NET 9 | 🟡 Migration scripts may need adjustment | Test migrations on .NET 10 preview |
| **PostgreSQL** | 17.x | 5-year support (until ~2029) | 🟢 Low risk | Annual minor version updates |
| **Redis** | 7.x | Community support active | 🟢 Low risk | Monitor Redis licensing changes (SSPL → source-available) |
| **React** | 19.x | Active LTS, Facebook-backed | 🟢 Low risk | Follow React team advisories |
| **Vite** | 7.x | Active, fast release cycle | 🟡 Breaking changes between majors | Pin major, upgrade quarterly |
| **Node.js** | (Vite runtime) | Track LTS releases | 🟢 Low risk if on LTS | Use LTS only |

### NuGet & npm Library Risk

| Library | Purpose | Maintenance Status | Risk |
|---------|---------|-------------------|------|
| `Serilog.*` | Structured logging | ✅ Active, multi-maintainer | 🟢 Low |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | JWT auth | ✅ Microsoft-maintained | 🟢 Low |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | Identity | ✅ Microsoft-maintained | 🟢 Low |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | PostgreSQL provider | ✅ Active, aligned with EF releases | 🟢 Low |
| `Microsoft.EntityFrameworkCore.Sqlite` | SQLite dev provider | ✅ Microsoft-maintained | 🟢 Low (should be removed from prod path) |
| `axios` | HTTP client (frontend) | ✅ Active | 🟢 Low |
| `react-dropzone` | File upload UI | 🟡 Moderate (less frequent releases) | 🟡 Medium — have fallback plan |
| `react-color` | Color picker | 🟡 Moderate | 🟡 Medium — lightweight wrapper, replaceable |

### .NET 9 → 10 LTS Migration Checklist

> **Deadline:** Before .NET 9 EOL (May 2026)

- [ ] Review .NET 10 breaking changes (preview release notes)
- [ ] Validate EF Core 10 migration compatibility
- [ ] Update Dockerfile base images (`mcr.microsoft.com/dotnet/aspnet:10.0`)
- [ ] Run full test suite on .NET 10 preview
- [ ] Update CI/CD pipeline target framework
- [ ] Verify Npgsql + SQLite provider compatibility
- [ ] Performance benchmark comparison

</details>

---

## 10. Gap Analysis: Current State vs. Future Goals

<details>
<summary><strong>Gap Tables</strong> — Scalability (5) + Reliability (6) + Maintainability (5)</summary>

### 10.1 Scalability Gaps

| Capability | Current | Needed for 100+ users | Gap |
|------------|---------|----------------------|-----|
| Database | SQLite (dev) / PostgreSQL (prod) | PostgreSQL only | SQLite cần loại bỏ khỏi production path |
| File Storage | Local disk (wwwroot/uploads) | Object storage (S3/Azure Blob) | IStorageService interface sẵn sàng, cần implement |
| SignalR | Single instance, user groups | Redis backplane | Chưa cấu hình |
| Caching | Redis (single instance) | Redis Cluster hoặc managed cache | Đủ cho single instance |
| Search | LIKE queries | Full-text search (PostgreSQL tsvector hoặc Elasticsearch) | Cần implement mới |

### 10.2 Reliability Gaps

| Capability | Current | Production Requirement | Gap |
|------------|---------|----------------------|-----|
| Testing | Zero | Unit + Integration minimum | **Critical gap** |
| CI/CD | None | Automated build/test/deploy | **Critical gap** |
| Monitoring | Health endpoint + Serilog | Metrics + Alerting + Tracing | Significant gap |
| Concurrency | Last-write-wins | Optimistic concurrency (RowVersion) | Medium gap |
| Backup | Manual pg_dump | Automated scheduled backup | Medium gap |
| HTTPS | HTTP only | TLS termination at proxy | Easy fix |

### 10.3 Maintainability Gaps

| Capability | Current | Sustainable Codebase | Gap |
|------------|---------|---------------------|-----|
| Frontend state | God Context (472 lines) | Focused contexts or Zustand | Refactor needed |
| App.jsx | 477 lines, ~50 destructured values | Container components by region | Refactor needed |
| ColorBoard.jsx | 555 lines | Extract sub-components | Low priority |
| API versioning | Hardcoded `/v1` prefix | Versioning strategy | Low priority now |
| Documentation | This doc + Swagger | ADR log + API changelog | Nice to have |

</details>

---

## 11. Recommended Improvements

### Priority Framework

Improvements được xếp hạng theo **impact × effort** matrix:

```
         HIGH IMPACT
              │
    P1 ───────┼─────── P2
   (Do first) │  (Plan next)
              │
  LOW EFFORT ─┼─ HIGH EFFORT
              │
    P3 ───────┼─────── P4
   (Quick win) │   (Defer)
              │
         LOW IMPACT
```

<details>
<summary><strong>Improvement Tables</strong> — P1 (6) + P2 (5) + P3 (4) + P4 (6)</summary>

### P1 — High Impact, Low-to-Medium Effort

| # | Item | Effort | Expected Outcome |
|---|------|--------|-----------------|
| 1 | **Add unit test project** (xUnit + Moq, cover service layer) | 2–3 days setup + ongoing | Safety net cho refactoring, catch regressions |
| 2 | **HTTPS enforcement** (Nginx TLS termination + HSTS header) | 0.5 day | Secure data in transit |
| 3 | **URL sanitization** (whitelist `http/https` schemes on backend) | 0.5 day | Block XSS via `javascript:` URLs |
| 4 | **Optimistic concurrency** (add `RowVersion` to Asset + Collection) | 1 day | Prevent silent data corruption |
| 5 | **Secret management** (environment variables hoặc Docker secrets, xóa secrets khỏi appsettings) | 0.5 day | Credentials không nằm trong source control |
| 6 | **CI pipeline** (GitHub Actions: build + test + lint) | 1 day | Automated quality gate |

### P2 — High Impact, High Effort

| # | Item | Effort | Expected Outcome |
|---|------|--------|-----------------|
| 7 | **Tách God Context** thành domain-specific contexts hoặc Zustand stores | 2–3 days | Eliminate re-render cascade, improve FE performance |
| 8 | **TanStack Query adoption** (replace manual useEffect+useState) | 3–5 days | Caching, dedup, background refetch, optimistic updates |
| 9 | **Integration test suite** (WebApplicationFactory + TestContainers) | 3–5 days setup | API contract verification |
| 10 | **Cloud storage implementation** (S3 hoặc Azure Blob via IStorageService) | 2–3 days | Unlock horizontal scaling |
| 11 | **CD pipeline** (auto-deploy to staging on merge, manual promote to prod) | 2–3 days | Eliminate manual deployment |

### P3 — Quick Wins

| # | Item | Effort | Expected Outcome |
|---|------|--------|-----------------|
| 12 | **Loại bỏ auto-migrate** cho production (explicit migration step) | 0.5 day | Safer deployments |
| 13 | **Environment-based API_URL** (VITE_API_URL build arg) | 0.5 day | Deploy staging/prod without code change |
| 14 | **N+1 query audit** (review `.Include()` strategies trong services) | 1 day | Giảm DB round-trips |
| 15 | **Swagger cho production** (read-only, behind auth) | 0.5 day | API documentation accessible |

### P4 — Defer

| # | Item | Effort | Notes |
|---|------|--------|-------|
| 16 | Full-text search (PostgreSQL tsvector) | 3–5 days | Chỉ cần khi data volume lớn |
| 17 | Repository pattern extraction | 5–7 days | Chỉ cần khi thêm nhiều entities |
| 18 | E2E tests (Playwright) | 3–5 days | Sau khi có unit + integration |
| 19 | CDN cho static assets | 1–2 days | Chỉ cần khi bandwidth là bottleneck |
| 20 | SignalR Redis backplane | 1 day | Chỉ cần cho multi-instance |
| 21 | API versioning strategy (beyond /v1) | 1–2 days | Chỉ cần khi có breaking changes planned |

</details>

---

## 12. Refactor Risk Analysis

Major proposed improvements (from Section 11) carry execution risk. This section quantifies blast radius to inform sequencing.

<details>
<summary><strong>Risk Table + Sequencing Constraints</strong></summary>

| Refactor | Blast Radius | Rollback Difficulty | Prerequisite | Risk Level |
|----------|-------------|--------------------|--------------|-----------|
| **Tách God Context → domain contexts** | All 17 components consume AppContext | Medium — can coexist during migration | Unit tests on hooks (safety net) | 🟡 Medium |
| **TanStack Query adoption** | All API call sites (11 hooks) | Low — migrate hook-by-hook | None, but benefits from context split first | 🟢 Low |
| **Cloud storage (S3/Azure)** | IStorageService swap, upload/download paths | Low — interface already abstracted | Integration tests for upload flow | 🟢 Low |
| **Optimistic concurrency (RowVersion)** | Asset + Collection entities, all write endpoints, frontend conflict handling | Medium — requires DB migration + API contract change | Client-side 409 handling | 🟡 Medium |
| **Repository pattern extraction** | All 12 services rewritten | High — cross-cutting change | Full unit test coverage first | 🔴 High |
| **CI/CD pipeline** | Build + deploy process | Low — additive, no code change | Docker builds must be reproducible | 🟢 Low |
| **Remove auto-migrate (prod)** | Startup sequence, deploy process | Low — env-gated | CI pipeline to run migrations explicitly | 🟢 Low |

### Sequencing Constraints

```
Unit Tests (safety net)
    │
    ├──▶ Tách God Context (needs tests to verify no regression)
    │       │
    │       └──▶ TanStack Query (benefits from smaller contexts)
    │
    ├──▶ Optimistic Concurrency (independent, but needs FE conflict UI)
    │
    ├──▶ Cloud Storage (independent, needs integration tests)
    │
    └──▶ Repository Pattern (LAST — highest risk, needs full coverage first)
```

**Key Insight:** Unit test coverage is the critical enabler. Without it, every structural refactor carries unquantifiable regression risk.

</details>

---

## 13. Strategic Roadmap

<details>
<summary><strong>3-Phase Roadmap</strong> — Stabilize (M1–2) → Modularize (M3–4) → Scale (M5–8)</summary>

### Phase 1: Stabilize (Month 1–2)

**Mục tiêu:** Thiết lập safety net và khắc phục các rủi ro critical.

```
Week 1-2: Test infrastructure
├── Add VAH.Backend.Tests project (xUnit + Moq)
├── Cover 5 core services: Asset, Collection, Auth, Tag, Permission
├── Setup CI pipeline (build + test on every PR)
└── Target: 60% service layer coverage

Week 3-4: Security & Reliability
├── HTTPS enforcement (Nginx TLS config)
├── URL sanitization (backend validation)
├── Optimistic concurrency (RowVersion on Asset, Collection)
├── Secret management (Docker secrets / env vars)
├── Explicit migration step (remove auto-migrate in production)
└── Environment-based frontend config (VITE_API_URL)
```

**Exit Criteria:**
- [ ] CI pipeline green on every merge
- [ ] ≥60% unit test coverage on service layer
- [ ] HTTPS enforced in production
- [ ] No secrets in source control
- [ ] Concurrency conflicts return 409

### Phase 2: Modularize (Month 3–4)

**Mục tiêu:** Cải thiện maintainability và developer experience.

```
Week 5-6: Frontend Architecture
├── Tách AppContext → AssetContext + CollectionContext + UIContext
├── Adopt TanStack Query (gradual migration, start with assets)
├── Extract App.jsx → container components per layout region
└── N+1 query audit + fix trên backend

Week 7-8: Backend Hardening
├── Integration tests (WebApplicationFactory + test DB)
├── CD pipeline (auto-deploy staging)
├── Monitoring: structured metrics (Prometheus-compatible)
└── Automated backup schedule for PostgreSQL
```

**Exit Criteria:**
- [ ] No single context/component >250 lines
- [ ] TanStack Query cho ≥80% API calls
- [ ] Integration tests cho critical API paths (auth, asset CRUD, permissions)
- [ ] One-command deploy to staging
- [ ] Automated daily backup

### Phase 3: Scale (Month 5–8)

**Mục tiêu:** Unlock horizontal scaling và production-grade observability.

```
Week 9-12: Infrastructure
├── Cloud storage implementation (S3/Azure via IStorageService)
├── SignalR Redis backplane
├── PostgreSQL full-text search (tsvector for assets + collections)
├── CDN for uploaded files
└── Multi-instance Docker Compose / Kubernetes manifest

Week 13-16: Observability & Quality
├── Distributed tracing (OpenTelemetry)
├── Alerting (on error rate, latency P95, disk usage)
├── E2E tests (Playwright, core flows)
├── Performance benchmarks (baseline for regression detection)
└── Load testing (k6/Artillery, target 100 concurrent users)
```

**Exit Criteria:**
- [ ] Multi-instance deployment verified
- [ ] ≥100 concurrent users sustained
- [ ] P95 latency <500ms for reads
- [ ] Alerting triggers on anomalies
- [ ] E2E tests cover login → upload → organize → share flow

</details>

---

## 14. Deployment Architecture

### Current: Single Instance

```
docker-compose.yml
├── postgres:17-alpine     (port 5432, healthcheck: pg_isready)
├── redis:7-alpine         (port 6379, healthcheck: redis-cli ping)
├── backend (multi-stage)  (port 5027, healthcheck: /api/v1/Health)
└── frontend (Nginx)       (port 80, SPA fallback)

Volumes: postgres-data, redis-data, backend-uploads, backend-logs
```

### Target: Scalable Production

```
                    Load Balancer (TLS termination)
                           │
              ┌────────────┼────────────┐
              ▼            ▼            ▼
         Backend ×N   Backend ×N   Backend ×N
              │            │            │
              └────────────┴────────────┘
                    │           │
              PostgreSQL    Redis Cluster
              (managed)     (backplane + cache)
                                │
                        Object Storage (S3)
                                │
                            CDN Edge
```

**Prerequisites cho target architecture:**
1. Cloud storage implementation (IStorageService already abstracted)
2. SignalR Redis backplane configuration
3. Externalized secrets (Key Vault / SSM)
4. Managed PostgreSQL (RDS / Cloud SQL)
5. Health check readiness endpoint (separate from liveness)

---

## 15. Environment Strategy

<details>
<summary><strong>Environment Matrix</strong> — Dev vs. Staging vs. Production (12 aspects)</summary>

| Aspect | Development | Staging | Production |
|--------|-------------|---------|------------|
| **Database** | SQLite (zero-config) | PostgreSQL (Docker) | PostgreSQL (managed RDS/Cloud SQL) |
| **Cache** | In-memory (no Redis) | Redis (Docker) | Redis (managed ElastiCache/Memorystore) |
| **Storage** | Local `wwwroot/uploads` | Local or S3 | S3 / Azure Blob |
| **Auth** | JWT (relaxed, long TTL for testing) | JWT (prod-like) | JWT (strict, HTTPS only) |
| **Migrations** | Auto-migrate on startup | Auto-migrate (gated) | **Explicit CLI step before deploy** |
| **Swagger** | Enabled | Enabled (read-only) | Disabled or behind auth |
| **HTTPS** | HTTP (localhost) | HTTPS (self-signed OK) | HTTPS (valid cert, HSTS) |
| **Secrets** | `appsettings.Development.json` | Environment variables | Docker secrets / Key Vault |
| **Logging** | Console + File (verbose) | Console + File (info) | Structured (JSON) + centralized sink |
| **Error Detail** | Full stack traces | Sanitized | ProblemDetails only (no internals) |
| **SignalR** | Single instance | Single instance | Redis backplane (if multi-instance) |
| **Monitoring** | None | Health endpoint | Health + Metrics + Alerting |

### Environment Parity Principle

Staging must mirror production infrastructure to catch environment-specific bugs (especially SQLite↔PostgreSQL drift). Dev may diverge for convenience but must run the full integration test suite against PostgreSQL before merge.

### Current Gap

**No staging environment exists.** Code goes from dev → production. This is the primary operational risk. Adding a staging environment (even as a second docker-compose profile) is a prerequisite for safe deployments.

</details>

---

## 16. Security Posture

### Implemented Controls

| Control | Implementation | Status |
|---------|---------------|--------|
| Authentication | JWT Bearer (HS256, 24h, ClockSkew=0) + ASP.NET Identity | ✅ |
| Authorization | `[Authorize]` on all endpoints (except Auth, Health) | ✅ |
| Data Isolation | UserId FK on all entities, enforced at service layer | ✅ |
| RBAC | Owner/Editor/Viewer roles per collection | ✅ |
| CORS | Config-driven origins, AllowCredentials for SignalR | ✅ |
| Rate Limiting | 100 req/min general, 20 req/min upload | ✅ |
| File Validation | Size (50MB), extension whitelist (27 types), MIME check | ✅ |
| Exception Privacy | Details only in Development environment | ✅ |
| Container Security | Non-root user, isolated volumes | ✅ |
| Password Policy | Min 6 chars, require digit + lowercase | ✅ |

### Outstanding Security Items

| Item | Severity | Remediation |
|------|----------|-------------|
| No HTTPS in default config | 🟠 High | TLS at Nginx layer + HSTS header |
| URL scheme not validated | 🟡 Medium | Whitelist `http/https` on link asset creation |
| JWT key in appsettings | 🟡 Medium | Move to environment variable / Docker secret |
| No brute-force protection beyond rate limit | 🟢 Low | Account lockout (Identity supports this) |
| JWT refresh token not implemented | 🟢 Low | Add refresh token endpoint for long sessions |

---

## 17. Threat Model Overview

STRIDE-based threat analysis for VAH’s current attack surface.

<details>
<summary><strong>STRIDE Analysis</strong> — 5 entry points + 9 threats + breach scenario + remediation</summary>

### Attack Surface Summary

| Entry Point | Protocol | Auth Required | Exposure |
|-------------|----------|---------------|----------|
| REST API (`/api/v1/*`) | HTTP(S) | Yes (JWT Bearer) | All CRUD operations, file upload/download |
| SignalR Hub (`/hubs/assets`) | WebSocket | Yes (JWT query param) | Real-time push events |
| Static files (`/assets/*`) | HTTP(S) | No | Uploaded files served via Nginx |
| Health endpoint (`/api/v1/Health`) | HTTP(S) | No | System status |
| Swagger UI (`/swagger`) | HTTP(S) | No (dev only) | API schema exposure |

### STRIDE Analysis

| Threat | Category | Attack Vector | Current Mitigation | Gap |
|--------|----------|---------------|-------------------|-----|
| **T1: Token theft** | Spoofing | XSS steals JWT from localStorage | Rate limiting, short-ish TTL (24h) | 🟠 24h is long; no refresh token rotation; localStorage is XSS-accessible |
| **T2: Privilege escalation** | Tampering | Modify JWT claims or bypass UserId filter | Server-side claim extraction, UserId enforcement at service layer | 🟢 Low risk — HS256 signature prevents claim tampering |
| **T3: Data exfiltration** | Info Disclosure | Enumerate assets via sequential IDs or missing authz | GUID IDs, per-user data isolation | 🟢 Low risk — GUIDs non-guessable, service layer filters by UserId |
| **T4: Stored XSS via link URLs** | Tampering | Save `javascript:` scheme URL as LinkAsset, rendered in UI | None | 🔴 **Open** — no URL scheme validation |
| **T5: File upload abuse** | Denial of Service | Upload large/many files to exhaust disk | 50MB limit, extension whitelist, MIME check | 🟡 Partial — no per-user quota, no virus scan |
| **T6: Static file access without auth** | Info Disclosure | Direct URL to uploaded file bypasses API auth | File naming uses GUID | 🟠 Obscured but not protected — anyone with URL can access |
| **T7: Credential exposure** | Info Disclosure | JWT key + DB password in `appsettings.json` | Dev-only (should be env vars in prod) | 🟠 No secret management in place |
| **T8: SQL injection** | Tampering | Malformed input in search/filter | EF Core parameterized queries | 🟢 Low risk — ORM prevents direct SQL injection |
| **T9: SignalR abuse** | Denial of Service | Flood WebSocket connections | ASP.NET Core connection limits, JWT required | 🟡 Basic — no per-user connection throttle |

### Breach Scenario: Compromised JWT Key

**Scenario:** Attacker obtains the HS256 signing key from source control or environment leak.

| Step | Impact | Detection |
|------|--------|----------|
| 1. Forge valid JWT for any user | Full account takeover | None (valid signature) |
| 2. Access/modify/delete all victim’s assets | Data loss, integrity breach | Audit log (not yet implemented) |
| 3. Escalate to admin if role claim is fabricated | Full system compromise | None |

**Mitigation plan:**
1. Move JWT key to Docker secret / Key Vault (blocks source control exposure)
2. Implement asymmetric signing (RS256) — private key never leaves server
3. Add token revocation list (Redis-backed) for emergency invalidation
4. Add audit logging for all write operations

### Priority Remediation

| Priority | Threat | Fix | Effort |
|----------|--------|-----|--------|
| P1 | T4 (XSS via URLs) | URL scheme whitelist on backend | 0.5 day |
| P1 | T7 (Credential exposure) | Move secrets to env vars / Docker secrets | 0.5 day |
| P2 | T6 (Static file access) | Proxy file serving through API with auth check | 1–2 days |
| P2 | T1 (Token theft) | Reduce TTL to 1h + add refresh token + HttpOnly cookie option | 1–2 days |
| P3 | T5 (Upload abuse) | Per-user storage quota | 1 day |
| P3 | T9 (SignalR abuse) | Per-user connection limit | 0.5 day |

</details>

---

## 18. Failure Mode & Incident Simulation

Production readiness requires knowing **HOW** things fail, not just what can go wrong. This section defines concrete failure scenarios, their blast radius, and expected system behavior. Missing from previous versions — added during pre-Series A due diligence review.

<details>
<summary><strong>Failure Modes (FM1–FM12) + 5 Simulation Playbooks + Missing Runbooks</strong></summary>

### Failure Mode Matrix

| # | Failure | Trigger | Current Behavior | Expected Behavior | Blast Radius | Recovery |
|---|---------|---------|-----------------|-------------------|-------------|----------|
| FM1 | **PostgreSQL down** | Container crash, disk full, OOM | Unhandled → 500 on all DB endpoints | Health endpoint RED, cached reads served, writes return 503 | 🔴 Total (all write + most read) | `docker-compose restart postgres` (~30s) |
| FM2 | **Redis down** | Container crash, memory cap | In-memory fallback activates silently | ✅ Already handled — but **no alert** that cache is degraded | 🟡 Perf degradation only | Auto-recovery on Redis restart |
| FM3 | **Disk full** (uploads volume) | Unbounded file growth, no quota | Upload fails with unclear IOException | Return 507 Insufficient Storage, alert on >80% disk | 🟠 Uploads blocked, reads OK | Expand volume or purge orphaned files |
| FM4 | **JWT key compromise** | Secret leaked from appsettings / env | Attacker forges tokens for any user | No detection. No revocation mechanism. | 🔴 Total auth bypass | Rotate key → all sessions invalidated simultaneously |
| FM5 | **Bad migration on startup** | Schema change fails mid-apply | App does not start → total outage, no rollback | Separate migration step. App starts on old schema if migration fails. | 🔴 Total outage | Fix migration SQL, re-run, restart |
| FM6 | **Memory leak in backend** | Long-running process, unbounded cache | OOM → container killed → Docker restart policy | GC pressure alert at 80% container memory limit | 🟡 Brief outage during restart | Docker auto-restart + healthcheck |
| FM7 | **SignalR hub overload** | Mass concurrent ops (bulk import by multiple users) | Backpressure not configured → potential queue overflow | Connection throttle, message batch coalescing | 🟡 Real-time delayed, API unaffected | Self-recovering |
| FM8 | **Concurrent writes to same entity** | Two users edit same collection/asset | Last-write-wins → **silent data loss** | Return 409 Conflict with RowVersion mismatch | 🟠 Data integrity risk | No automated resolution |
| FM9 | **Orphaned files on disk** | Asset deleted from DB, file remains | Storage leak — disk grows monotonically | Background cleanup job or cascade-delete hook | 🟢 No user impact, ops cost | Manual cleanup script |
| FM10 | **Cascading delete of large collection** | Delete collection with 1000+ assets | Long transaction, potential timeout, partial delete | Soft-delete + background hard-delete in batches | 🔴 Data loss if partial | Restore from backup (if it exists) |
| FM11 | **Image processing failure** | Corrupt file, unsupported format, OOM on large image | Thumbnail generation throws → asset saved without thumbnails | Graceful degradation: placeholder thumbnail, retry queue | 🟡 Visual degradation | Manual re-trigger or accept placeholder |
| FM12 | **Network partition between services** | Docker bridge network failure | Backend cannot reach Postgres/Redis simultaneously | Circuit breaker pattern: fast-fail, queue writes, serve stale cache | 🔴 Cascading failures | Docker network restart |

### Incident Simulation Playbook

Run these exercises **before first production deployment** and quarterly thereafter.

#### Sim 1: Database Failure Recovery
```
1. docker-compose stop postgres
2. Verify: health endpoint returns {"status": "Unhealthy"}
3. Verify: API returns 503 Service Unavailable (NOT 500 with stack trace)
4. Verify: frontend shows degraded-mode banner (NOT white screen)
5. docker-compose start postgres
6. Verify: auto-recovery within 60s, no data corruption
7. Verify: all pending SignalR connections re-establish
```
**Current gap:** Step 3 → likely returns 500 with unhandled `NpgsqlException`. Step 4 → frontend has no error boundary for API-down state.

#### Sim 2: Disk Exhaustion
```
1. Fill uploads volume to 95% capacity
2. Attempt file upload (50MB image)
3. Verify: meaningful error returned (507), not generic 500
4. Verify: non-upload operations (CRUD, search, auth) unaffected
5. Verify: monitoring alert fires (when implemented)
6. Clean up, verify uploads resume
```
**Current gap:** No disk monitoring. Error surfaces as unhandled `IOException`.

#### Sim 3: Token Revocation / Key Rotation
```
1. Login as user A, obtain JWT
2. Rotate JWT secret (change environment variable)
3. Restart backend
4. Attempt API call with old token
5. Verify: 401 Unauthorized (NOT 500)
6. Verify: re-login with credentials works immediately
```
**Current gap:** No graceful key rotation. All active sessions terminated simultaneously. No overlap period for old+new keys.

#### Sim 4: Concurrent Modification
```
1. Open two browser sessions, same collection
2. Simultaneously: Tab A adds asset, Tab B renames collection
3. Verify: both succeed OR second gets 409 Conflict
4. Verify: no silent data loss
5. Verify: SignalR delivers update to both tabs
```
**Current gap:** Last-write-wins. No 409. Silent data hazard.

#### Sim 5: Backup & Restore Drill
```
1. Create test data (collection + 10 assets + tags)
2. Execute pg_dump
3. Destroy database (docker-compose down -v)
4. Restore from dump
5. Verify: all data present, file references intact
6. Measure: total time from "disaster" to "recovered"
```
**Current gap:** ❌ **This drill has never been executed.** There is no documented restore procedure. RPO is effectively ∞.

### Missing Operational Runbooks

| Scenario | Runbook Exists? | Priority | Estimated Effort |
|----------|----------------|----------|-----------------|
| Database restore from backup | ❌ | **P0 — existential risk** | 0.5 day to write + validate |
| Secret rotation (JWT key, DB password) | ❌ | P1 | 0.5 day |
| Container health degradation triage | ❌ | P2 | 0.5 day |
| Storage capacity emergency | ❌ | P2 | 0.5 day |
| Performance investigation (slow queries) | ❌ | P3 | 1 day |
| User data deletion request (GDPR-style) | ❌ | P3 | 1 day |
| Incident post-mortem template | ❌ | P2 | 0.5 day |

> **Due diligence finding:** A startup that cannot restore its own database from backup is one `DROP TABLE` away from total business failure. Write the backup runbook this week.

</details>

---

## 19. Performance Profile

### Current Optimizations

| Strategy | Detail | Scope |
|----------|--------|-------|
| Redis Cache | Collection list: 5min absolute / 2min sliding TTL | Backend |
| In-Memory Fallback | Automatic when Redis unavailable | Backend |
| Thumbnail Pre-gen | sm(150px) / md(400px) / lg(800px) WebP, quality 80 | Backend |
| Nginx Cache | `/assets/` 1-year immutable, gzip enabled | Infra |
| DB Indexes | ~22 indexes on FK and common query patterns | Database |
| Pagination | Server-side, max 100 items/page | API |

### Known Performance Risks

| Risk | Trigger | Mitigation |
|------|---------|------------|
| N+1 queries | Nested `.Include()` patterns in services | Audit + explicit loading strategy |
| LIKE search degradation | >10K assets | PostgreSQL tsvector full-text search |
| Frontend re-render cascade | God Context, any state mutation | Context splitting / Zustand migration |
| File serving bottleneck | Many concurrent downloads | CDN + object storage offload |

---

## 20. Service Level Objectives (SLOs)

Targets: 99.5% uptime, <500ms P95 latency, <0.5% error rate. **Critical gap:** RPO = ∞ (no automated backup).

<details>
<summary><strong>SLO Tables</strong> — Availability, Error Budget, Recovery, Implementation Roadmap</summary>

### Availability & Latency

| Metric | Target | Measurement Method | Current Estimate | “good enough” for the current user base (individual creators, small teams). Revisit when user count exceeds Mode B threshold (50–100 concurrent).

### Availability & Latency

| Metric | Target | Measurement Method | Current Estimate |
|--------|--------|-------------------|------------------|
| **Uptime** | 99.5% (3.65h downtime/month) | Health endpoint polling (1-min interval) | Unknown — no monitoring |
| **API P50 latency** | <100ms | Serilog request timing | ~50–80ms (single instance, low load) |
| **API P95 latency** | <500ms | Serilog request timing | Unknown — no percentile tracking |
| **API P99 latency** | <2000ms | Serilog request timing | Unknown |
| **Upload P95** | <5s (50MB file) | Application-level timer | ~3–4s local network |
| **SignalR delivery** | <500ms from mutation to client | Event timestamp delta | Unknown |

### Error Budget

| Metric | Target | Notes |
|--------|--------|-------|
| **Error rate (5xx)** | <0.5% of total requests | Excludes client errors (4xx) |
| **Failed uploads** | <1% | Network/validation failures |
| **SignalR disconnects** | <5% reconnection rate per session | Auto-reconnect should handle transparently |

### Recovery Objectives

| Metric | Target | Current Capability | Gap |
|--------|--------|--------------------|-----|
| **RTO** (Recovery Time Objective) | <30 min | ~10–15 min (`docker-compose up`) | ✅ Achievable for single instance |
| **RPO** (Recovery Point Objective) | <24h | Manual `pg_dump` (unscheduled) | ❌ No automated backup → RPO = ∞ |
| **MTTR** (Mean Time To Repair) | <1h | No runbook beyond basics | 🟡 Needs incident response process |

### SLO Implementation Roadmap

1. **Phase 1 (Stabilize):** Instrument Serilog for latency percentiles. Add health endpoint monitoring.
2. **Phase 2 (Modularize):** Automated daily backup (RPO <24h). Basic alerting on 5xx rate.
3. **Phase 3 (Scale):** Full SLO dashboard (Prometheus + Grafana). Error budget tracking. PagerDuty/Slack alerts.

</details>

---

## 21. Observability Architecture

### Current State (Tier 0)

```
[Serilog Console+File] ─── Manual log reading
[Health Endpoint]      ─── Docker healthcheck (binary up/down)
```

**Gaps:** No metrics, no tracing, no alerting, no dashboards. Issues discovered by users, not by system.

<details>
<summary><strong>Target Observability Stack</strong> (Tier 2) + Implementation Path</summary>

### Target State (Tier 2)

```
┌──────────────────────────────────────────────────────────┐
│  METRICS (Prometheus-compatible)                          │
│  ─ HTTP request rate, latency histogram, error rate       │
│  ─ Active SignalR connections                              │
│  ─ Upload throughput (files/min, bytes/min)                │
│  ─ Cache hit/miss ratio                                    │
│  ─ DB query duration (EF Core diagnostics)                 │
├──────────────────────────────────────────────────────────┤
│  TRACING (OpenTelemetry → Jaeger/Zipkin)                  │
│  ─ HTTP request → service call → DB query spans            │
│  ─ Correlation ID propagated via headers                   │
│  ─ SignalR event traces                                    │
├──────────────────────────────────────────────────────────┤
│  LOGGING (Serilog → structured JSON)                      │
│  ─ Existing Console + File sinks retained                  │
│  ─ Add: Seq or Elasticsearch sink for search/aggregation   │
│  ─ Correlation ID in every log entry                       │
├──────────────────────────────────────────────────────────┤
│  ALERTING                                                │
│  ─ 5xx rate >1% for 5 min → Slack/PagerDuty               │
│  ─ P95 latency >2s for 10 min → warning                   │
│  ─ Health check failure ×3 → critical                      │
│  ─ Disk usage >80% → warning (upload volume)               │
│  ─ Redis connection lost → warning (cache fallback active)  │
└──────────────────────────────────────────────────────────┘
```

### Implementation Path

| Phase | Add | Effort | Dependency |
|-------|-----|--------|------------|
| Stabilize | Serilog latency enricher + Health endpoint monitoring script | 0.5 day | None |
| Modularize | Prometheus metrics via `prometheus-net` + Grafana dashboard | 1–2 days | Docker Compose service |
| Scale | OpenTelemetry tracing + Seq/ELK log aggregation + PagerDuty integration | 3–5 days | Managed infra recommended |

</details>

---

## 22. Data Growth & Capacity Planning

<details>
<summary><strong>Projections</strong> — Storage, DB Performance, SignalR, Capacity Thresholds</summary>

### Storage Projections

Assumptions: Average image = 2MB, average thumbnail set (sm+md+lg) = 150KB, average assets per user = 200.

| Metric | 100 Users | 1,000 Users | 10,000 Users |
|--------|-----------|-------------|---------------|
| **Total assets** | 20K | 200K | 2M |
| **Image storage** | ~40 GB | ~400 GB | ~4 TB |
| **Thumbnail storage** | ~3 GB | ~30 GB | ~300 GB |
| **Database size** | ~50 MB | ~500 MB | ~5 GB |
| **Storage solution** | Local disk | Local disk / S3 | S3 / Azure Blob (mandatory) |
| **Backup size** | Trivial | ~500 MB/day | ~5 GB/day |

### Database Performance Projections

| Scale | Assets Table Rows | LIKE Search Latency | Index Size | Recommended Action |
|-------|-------------------|--------------------|-----------|-----------------|
| 100 users | 20K | <50ms | ~5 MB | No changes needed |
| 1K users | 200K | 200–500ms | ~50 MB | **Add PostgreSQL tsvector** |
| 10K users | 2M | >2s (unacceptable) | ~500 MB | tsvector + read replicas |

### SignalR Connection Projections

| Scale | Concurrent Connections | Memory per Connection | Total Memory | Action |
|-------|-----------------------|----------------------|-------------|--------|
| 50 users | ~30 | ~50 KB | ~1.5 MB | Single instance fine |
| 500 users | ~300 | ~50 KB | ~15 MB | Single instance OK |
| 5K users | ~3,000 | ~50 KB | ~150 MB | **Redis backplane required** |

### Capacity Thresholds (Action Triggers)

| Indicator | Threshold | Action |
|-----------|-----------|--------|
| Upload volume disk usage | >70% | Migrate to object storage or expand volume |
| Assets table >100K rows | LIKE queries >200ms | Implement tsvector full-text search |
| Concurrent SignalR connections >500 | Memory pressure | Add Redis backplane |
| Database size >1 GB | Backup takes >5 min | Move to managed DB with automated backup |
| Concurrent write operations >50/sec | SQLite contention (dev) | Ensure production uses PostgreSQL only |

</details>

---

## 23. Cost Projection

Rough infrastructure cost modeling at different user scales. Costs are monthly estimates based on AWS pricing (Azure/GCP comparable).

<details>
<summary><strong>Cost Tables</strong> — Infrastructure by Scale + Cost Drivers + Break-Even + Self-Hosted</summary>

### Infrastructure Cost by Scale

| Component | 100 Users | 1,000 Users | 10,000 Users |
|-----------|-----------|-------------|---------------|
| **Compute** (backend instances) | 1× t3.small ($15) | 2× t3.medium ($60) | 4× t3.large ($240) |
| **Database** (PostgreSQL) | t3.micro RDS ($15) | db.t3.medium ($65) | db.r5.large ($200) + read replica ($200) |
| **Redis** (cache + backplane) | t3.micro ElastiCache ($13) | cache.t3.small ($25) | cache.r5.large ($150) |
| **Object Storage** (S3) | 40 GB ($1) | 400 GB ($9) | 4 TB ($92) |
| **Data Transfer** (egress) | ~10 GB ($1) | ~100 GB ($9) | ~1 TB ($90) |
| **CDN** (CloudFront) | — (not needed) | Basic ($5) | Full ($50–100) |
| **Monitoring** (CloudWatch / Grafana Cloud) | Free tier | Basic ($10) | Full ($50) |
| **Backup** (DB snapshots + S3 versioning) | ~$2 | ~$15 | ~$100 |
| **SSL Certificate** (ACM) | Free | Free | Free |
| **Domain + DNS** | $12/yr ≈ $1 | $1 | $1 |
| **Total Monthly** | **~$48** | **~$200** | **~$1,125** |
| **Per-user/month** | **$0.48** | **$0.20** | **$0.11** |

### Cost Drivers Analysis

| Driver | % of Total (at 1K users) | Scaling Behavior | Optimization |
|--------|--------------------------|------------------|--------------|
| Compute | ~30% | Linear (instance count) | Right-size instances, autoscaling |
| Database | ~33% | Step-function (upgrade tiers) | Read replicas, query optimization, connection pooling |
| Storage | ~5% | Linear (user content) | Lifecycle policies, compression, tiered storage |
| Cache | ~13% | Step-function | Eviction tuning, cache-aside pattern |
| Data Transfer | ~5% | Linear (user activity) | CDN offload, image compression, lazy loading |

### Break-Even Analysis

If VAH were offered as SaaS:
- At **$5/user/month**: Break-even at ~40 users (covers $200 infra)
- At **$10/user/month**: Profitable from ~20 users
- At **$3/user/month**: Break-even at ~67 users

> **Note:** Personnel cost (developer time) dominates total cost of ownership at this scale. Infrastructure is <10% of total cost when factoring in 1 FTE developer.

### Self-Hosted Option

For users running VAH on their own infrastructure:

| Scale | Minimum Hardware | Estimated Cost |
|-------|-----------------|----------------|
| Personal (1 user) | Raspberry Pi 4 / old laptop | ~$0 (existing hardware) |
| Small team (5–20) | 4-core / 8GB VPS | $20–40/month |
| Organization (50–100) | 8-core / 32GB dedicated | $80–150/month |

</details>

---

## 24. Architectural Pivot Triggers

Conditions that indicate the current monolith architecture needs fundamental restructuring. These are **not aspirational goals** — they are breakpoints where the current architecture will fail without intervention.

<details>
<summary><strong>Trigger Matrix + Decision Framework</strong> — 9 pivot triggers PT1–PT9 + Monolith→Modular→Microservices</summary>

### Trigger Matrix

| # | Indicator | Threshold | Current Value | Pivot Action |
|---|-----------|-----------|---------------|-------------|
| PT1 | **Entity count** (distinct EF types) | >30 entities | ~10 entities | Extract bounded contexts into separate modules with explicit interfaces |
| PT2 | **API endpoint count** | >80 endpoints | ~47 endpoints | Introduce API gateway pattern, split into domain-specific backends |
| PT3 | **Concurrent users** | >200 sustained | Unknown (≤1) | Horizontal scaling: load balancer + stateless backend + external state |
| PT4 | **Request throughput** | >500 req/sec sustained | Unknown (<50) | Add caching layer, read replicas, consider CQRS for all domains |
| PT5 | **Team size** | >3 developers on same codebase | 1 developer | Split into domain-owned modules to reduce merge conflicts |
| PT6 | **Deployment frequency** | >3x/day to production | Manual (≤1x/week) | Feature flags, canary deployment, service mesh |
| PT7 | **Single entity table rows** | >5M rows in any table | <1K | Table partitioning, archival strategy, read replicas |
| PT8 | **File storage volume** | >1 TB in local disk | <1 GB | **Mandatory pivot to object storage** (S3/Azure Blob) |
| PT9 | **Frontend bundle size** | >2 MB gzipped | ~200 KB | Code splitting, lazy routes, micro-frontend consideration |

### Decision Framework: Monolith → Modular → Microservices

```
┌─────────────────────┐     ┌─────────────────────┐     ┌─────────────────────┐
│  Current Monolith   │ ───▶ │  Modular Monolith   │ ───▶ │  Microservices      │
│  (VAH today)        │     │  (target Phase 2)   │     │  (if needed)        │
└─────────────────────┘     └─────────────────────┘     └─────────────────────┘
  Trigger: PT1, PT5           Trigger: PT2, PT3, PT4        Trigger: PT5 (>8),
  (>30 entities or             (>200 users or                 PT6 (>3x/day)
   >3 developers)               >500 req/sec)
```

</details>

### Current Assessment

VAH is **well within monolith territory**. No pivot triggers are close to activation. The current focus should be on:
1. **Stabilizing** the monolith (tests, CI/CD, security hardening)
2. **Preparing interfaces** for future pivots (IStorageService ✓, SignalR abstraction ✓)
3. **Not prematurely optimizing** — premature microservices would add complexity without benefit

Revisit this section when any pivot trigger reaches 60% of its threshold.

---

## 25. 2-Year Architectural North Star (2026–2028)

This is **NOT** a feature roadmap. It is a technical architecture target that governs every infrastructure decision. If an increment moves toward this target, approve it. If it moves away, demand justification via ADR.

### Vision Statement

> By Q1 2028, VAH operates as a **multi-instance, horizontally scalable SaaS platform** serving ≤10,000 users with 99.9% uptime, zero-downtime deployments, and <200ms P95 API latency — while maintaining a codebase that a new developer can understand and ship to production within one week.

<details>
<summary><strong>Target Architecture + Milestones + Anti-Goals + Decision Checkpoints</strong></summary>

### Target Architecture (Q1 2028)

```
┌──────────────────────────────────────────────────────────────┐
│                    CDN (static assets + thumbnails)           │
└──────────────────────────┬───────────────────────────────────┘
                           │
┌──────────────────────────▼───────────────────────────────────┐
│              API Gateway / Load Balancer (TLS)                │
│              Rate limiting · Auth pre-validation · Routing    │
└─────┬──────────────────┬─────────────────────┬───────────────┘
      ▼                  ▼                     ▼
┌─────────────┐  ┌──────────────┐     ┌──────────────────┐
│ Backend ×N  │  │ Backend ×N   │     │ Background Worker │
│ (stateless) │  │ (stateless)  │     │ (thumbnails,      │
│             │  │              │     │  cleanup, search   │
│             │  │              │     │  indexing)         │
└──────┬──────┘  └──────┬───────┘     └──────┬────────────┘
       │                │                     │
       └────────────────┴─────────────────────┘
                        │
       ┌────────────────┼────────────────┐
       ▼                ▼                ▼
  PostgreSQL        Redis Cluster    Object Storage
  (managed,         (cache +         (S3 / Azure Blob)
   read replicas)    backplane)
```

### Technical Milestones

| Quarter | Milestone | Success Metric | Dependency |
|---------|-----------|---------------|------------|
| Q2 2026 | **Foundation:** CI/CD + 70% test coverage + HTTPS + secrets mgmt | CI green on every merge, SonarQube quality gate pass | None |
| Q3 2026 | **Data Layer:** Cloud storage + PostgreSQL-only (remove SQLite) + optimistic concurrency | Horizontal scaling unblocked, zero dual-provider bugs | Q2 foundation |
| Q4 2026 | **Frontend Modernization:** Context split + TanStack Query + code splitting | Largest component <250 LOC, P95 re-render <16ms | Test coverage for safe refactor |
| Q1 2027 | **Observability:** Prometheus + Grafana + OpenTelemetry + alerting | MTTD <5min for production incidents | Infra maturity |
| Q2 2027 | **Multi-Instance:** Load balancer + SignalR backplane + stateless backend | 2+ instances, zero-downtime deploy | Cloud storage + Redis backplane |
| Q3 2027 | **Search & Performance:** PostgreSQL tsvector + query optimization + CDN | Search <100ms at 200K assets, CDN hit rate >80% | PostgreSQL-only stack |
| Q4 2027 | **Operational Maturity:** Automated DR drill, runbooks, on-call rotation | RTO <15min (verified), RPO <1h | Observability + backup |
| Q1 2028 | **Scale Validation:** Load test 10K users, security pen-test, perf budget | P95 <200ms, 99.9% uptime over 30 days | All above |

### Anti-Goals for 2 Years

| We will NOT | Because |
|-------------|---------|
| Extract microservices | Monolith serves ≤10K users. Network overhead + ops complexity not justified. See §2 C3. |
| Build mobile native app | Responsive web covers the use case. See §2 C6. |
| Implement CQRS everywhere | Partial CQRS (Assets) is sufficient. Full CQRS adds event sourcing complexity without proportional read/write divergence. |
| Support self-hosted deployment | Focus on SaaS. Self-hosted complicates versioning, support, and feature gating. |
| Add GraphQL or gRPC | REST is sufficient at <15 entities and <50 endpoints. See §2 C9. |

### Decision Checkpoints

These are **kill-or-invest** gates. If the business signal is absent, freeze architecture investment and focus on product.

| Checkpoint | Date | Signal | If YES | If NO |
|------------|------|--------|--------|-------|
| Product-Market Fit | Q3 2026 | >50 paying users? | Invest in Scale (Phase 3 roadmap §13) | Pivot product, freeze infra spend |
| Scale Signal | Q1 2027 | >500 users or >100 concurrent? | Prioritize multi-instance + CDN | Continue single-instance, invest in features |
| Team Growth | Q2 2027 | >3 developers? | Enforce module boundaries, split CI ownership | Keep monolith, shared ownership |
| Platform Play | Q4 2027 | API consumers beyond own frontend? | API versioning, developer portal, rate tiers | Internal API only, skip versioning |
| Series B Readiness | Q1 2028 | Revenue + growth justify next round? | Full scale validation, security audit | Maintain steady state |

</details>

---

## 26. Optimistic Assumption Critique

A pre-Series A due diligence review must challenge hidden assumptions. The following assumptions permeate this document and should be explicitly stress-tested. **Being wrong about any of these could cause material harm.**

<details>
<summary><strong>Assumption Critiques A1–A7</strong> — Cloud migration, SQLite drift, backup risk, rate limiting, Docker Compose, RBAC, timeline</summary>

### A1: "IStorageService abstraction makes cloud migration easy"

**Claim (§11, §14):** Cloud storage is a 2–3 day swap because the interface is already abstracted.

**Critique:** The interface guarantees method signature compatibility, not behavioral compatibility. Unaddressed concerns:
- **URL generation:** Local storage serves files via Nginx static path. S3 requires pre-signed URLs with expiry — fundamentally different URL lifetime semantics.
- **Thumbnail pipeline:** Currently writes thumbnails to local disk synchronously. Cloud version needs streaming upload to S3 — different I/O model.
- **Atomic operations:** Local file delete + DB delete within same request is quasi-atomic. S3 DELETE + DB transaction is not — orphaned files or dangling references on partial failure.
- **Cost model surprise:** S3 charges per PUT/GET request. High-frequency thumbnail access (3 sizes × every asset view) may produce unexpected egress costs.
- **Migration of existing data:** All existing uploads in `wwwroot/uploads` need a data migration script. Not accounted for in the 2–3 day estimate.

**Realistic estimate:** 5–7 days including edge cases and data migration, not 2–3.

### A2: "SQLite→PostgreSQL drift is manageable"

**Claim (§15):** Dual provider is acceptable with `DatabaseProviderInfo` dialect handling.

**Critique:** This is the **most dangerous assumption** in the architecture. Known behavioral differences:
- `DateTime` handling: SQLite stores as ISO string, PostgreSQL as `timestamp with time zone` — timezone bugs
- Case sensitivity: SQLite LIKE is case-insensitive by default, PostgreSQL LIKE is case-sensitive — search behavior differs
- Transaction isolation: SQLite has database-level locking, PostgreSQL has row-level — concurrency bugs invisible in dev
- JSON column support syntax differs between providers
- Cascade behavior under concurrent load differs

**Risk:** `DatabaseProviderInfo` only guards KNOWN differences. Every "works on my machine" production bug is rooted here. You are maintaining two databases with a team of one.

**Recommendation:** Remove SQLite from the dev path. Use `docker compose up postgres` for dev. Cost: 1–2 days. ROI: eliminates an entire class of production-only bugs permanently. This is the single highest-ROI change in the backlog.

### A3: "Zero test coverage is the #1 risk"

**Claim (§8, Executive Summary):** Testing is the highest risk.

**Critique:** Testing is the most VISIBLE risk, but not necessarily the highest IMPACT risk. The actual #1 existential risk is: **no backup and no verified recovery procedure.**

- With zero tests, you ship bugs. Bugs are fixable.
- With zero backups, you lose everything. Data loss is permanent.
- A corrupted database, an accidental `DELETE FROM assets`, or a ransomware attack on the server means **total, permanent, unrecoverable business failure**.
- You can rebuild code from git. You cannot rebuild user data from nothing.

**Correction:** Reclassify automated backup + verified restore as **equal severity to testing**. Move backup runbook to Phase 1, Week 1 — before writing the first unit test. A `pg_dump` cron job + tested restore procedure costs 0.5 days and provides existential insurance.

### A4: "Rate limiting prevents abuse"

**Claim (§16):** Rate limiting at 100 req/min general, 20 req/min upload provides protection.

**Critique:** Rate limiting without per-user quotas is a speed bump, not a barrier.
- **Storage abuse:** 20 uploads/min × 50MB = 1 GB/minute. In one day: **1.4 TB**. Rate limit doesn't prevent storage exhaustion — it merely slows the attacker.
- **Account enumeration:** 100 req/min to login endpoint = 6,000 password attempts per hour per IP. No per-account throttle (exponential backoff after N failures).
- **Free tier abuse (SaaS):** Without per-user storage quotas, a single user on a free tier can consume unlimited storage.

**Recommendation:** Per-user storage quota (enforce at service layer). Per-account login throttle (ASP.NET Identity supports lockout). Global rate limit on unauthenticated endpoints.

### A5: "Docker Compose is sufficient for production"

**Claim (§14):** Docker Compose for single-instance deployment.

**Critique:** Docker Compose is a development orchestrator, not a production one.
- **No rolling updates:** `docker-compose up` restarts all containers simultaneously → downtime on every deploy.
- **No resource limits by default:** Memory leak in backend takes down the entire host, including the database.
- **No service replacement:** If a container persistently fails health checks, Compose just keeps restarting it. No replacement, no drain, no rollback.
- **No secrets management:** Compose files reference plaintext environment variables or `.env` files.

**For Series A:** Minimum viable production = Docker Compose with explicit resource limits (`mem_limit`, `cpus`) + `docker-compose.prod.yml` overlay + external secret injection. Target: migrate to Docker Swarm (simplest step up) or managed Kubernetes within 6 months of first paying customer.

### A6: "RBAC is implemented"

**Claim (§16):** Owner/Editor/Viewer roles per collection.

**Critique:** RBAC is implemented at the **collection level only**. For a SaaS product, this is incomplete:
- ❌ No workspace-level roles (admin vs. member vs. billing)
- ❌ No permission inheritance (sub-collection doesn't inherit parent permissions)
- ❌ No permission on individual assets (only entire collections)
- ❌ No audit trail for permission changes (who gave whom access when?)
- ❌ No time-limited access (share link with expiry)
- ❌ No permission revocation verification

For Series A due diligence, investors will ask: *"Can an admin revoke access and prove it?"* Today: "Partially, and we can't prove it."

### A7: "The roadmap timeline is realistic"

**Claim (§13):** Phase 1 in 2 months, Phase 2 in 2 months, Phase 3 in 4 months — 8 months total.

**Critique:** These timelines assume:
- A developer who does **nothing but infrastructure** (no feature development, no bug fixes, no support)
- No regressions during refactoring (with zero test coverage as starting point)
- No scope creep from product/business needs
- No DevOps learning curve (CI/CD, monitoring, Kubernetes are new to this team)
- No context switching between infrastructure and feature work

**Realistic estimate:** Apply 1.5–2× multiplier when feature development competes for engineering time. Phase 1: 3–4 months. Phase 2: 3–4 months. Phase 3: 6–8 months. **Total: 12–16 months, not 8.**

### Summary of Bias Corrections

| Assumption | Stated | Corrected | Impact |
|-----------|--------|-----------|--------|
| Cloud storage migration | 2–3 days | 5–7 days | Underestimated by 2× |
| SQLite drift risk | Manageable | Dangerous — remove ASAP | Class of production-only bugs |
| #1 risk | Zero tests | Zero backup (equally critical) | Existential risk unaddressed |
| Rate limiting | Prevents abuse | Slows abuse | Storage exhaustion, brute force |
| Docker Compose | Production-ready | Dev-grade orchestrator | Downtime on every deploy |
| RBAC | Implemented | Partially implemented | Audit/compliance gap |
| Roadmap | 8 months | 12–16 months | Resource planning error |

</details>

---

## 27. API Surface Summary

Canonical reference: **Swagger UI at `/swagger`**

| Domain | Controller | Endpoints | Auth |
|--------|-----------|-----------|------|
| Assets | AssetsCommandController + AssetsQueryController | 15 (CRUD, upload, duplicate, reorder, domain-create) | JWT |
| Bulk Ops | BulkAssetsController | 4 (bulk-delete, bulk-move, bulk-move-group, bulk-tag) | JWT |
| Collections | CollectionsController | 6 (CRUD + items) | JWT |
| Auth | AuthController | 2 (register, login) | Rate-limited |
| Search | SearchController | 1 (multi-field search) | JWT |
| Tags | TagsController | 10 (CRUD + asset-tag M2M + migration) | JWT |
| Smart Collections | SmartCollectionsController | 2 (list + items) | JWT |
| Permissions | PermissionsController | 6 (RBAC CRUD + my-role + shared-collections) | JWT |
| Health | HealthController | 1 (DB + storage check) | Public |

---

## 28. Source of Truth

| Artifact | Location |
|----------|----------|
| API contracts | Swagger UI at `/swagger` (runtime) |
| Database schema | `VAH.Backend/Migrations/` + `AppDbContextModelSnapshot.cs` |
| Backend architecture | `VAH.Backend/` folder structure |
| Frontend architecture | `VAH.Frontend/src/` folder structure |
| Deployment configuration | `docker-compose.yml` + Dockerfiles |
| This assessment | `docs/ARCHITECTURE_REVIEW.md` |
| Data flow diagrams + Service dependency graph | `docs/PROJECT_DOCUMENTATION.md` §1 |
| Technical reference (models, services, controllers, components) | `docs/PROJECT_DOCUMENTATION.md` §2–8 |
| Developer onboarding, setup, troubleshooting | `docs/IMPLEMENTATION_GUIDE.md` §1–8 |
| Rollback strategy + Backup/Restore procedures | `docs/IMPLEMENTATION_GUIDE.md` §9–10 |
| Compliance & regulatory statement | `docs/IMPLEMENTATION_GUIDE.md` §11 |
| OOP assessment + refactoring log | `docs/OOP_ASSESSMENT.md` |
| Development changelog (session history) | `docs/FIX_REPORT_20260227.md` |

---

## 29. Appendix

### A. Glossary

| Term | Definition |
|------|-----------|
| **Asset** | Digital resource (image, link, color, folder, color-group) managed via TPH inheritance |
| **Collection** | Hierarchical container for assets (supports parent–child nesting) |
| **Smart Collection** | Virtual view based on dynamic filters (recent, untagged, by-tag, etc.) |
| **RBAC** | Role-Based Access Control: Owner (full), Editor (read/write), Viewer (read-only) |
| **TPH** | Table-Per-Hierarchy: single DB table with discriminator column for entity inheritance |
| **Feature Slice** | Vertical architecture pattern separating Command and Query controllers |
| **God Context** | Anti-pattern: single React context holding all application state |
| **SLO** | Service Level Objective: measurable target for system reliability (uptime, latency, error rate) |
| **RTO** | Recovery Time Objective: maximum acceptable downtime after a failure |
| **RPO** | Recovery Point Objective: maximum acceptable data loss measured in time |
| **Tactical Debt** | Quick-fix shortcuts with low blast radius, cheap to repay |
| **Structural Debt** | Architecture-level issues requiring refactoring to resolve |
| **Operational Debt** | Gaps in deployment, monitoring, and reliability infrastructure |
| **Strategic Debt** | Issues that block future growth trajectories |
| **ADR** | Architecture Decision Record: lightweight document capturing a significant architectural decision, its context, and trade-offs |
| **STRIDE** | Threat modeling framework: Spoofing, Tampering, Repudiation, Information Disclosure, Denial of Service, Elevation of Privilege |
| **Bounded Context** | DDD concept: a logical boundary within which a domain model is consistent and autonomous |
| **Pivot Trigger** | A measurable threshold that, when reached, indicates the current architecture needs fundamental restructuring |
| **STS** | Standard Term Support: .NET release with 18-month support lifecycle (vs. 3-year LTS) |
| **Context Map** | DDD diagram showing relationships and integration patterns between bounded contexts |
| **Core Domain** | The part of the system that provides competitive advantage and justifies custom development |
| **Break-Even Point** | The user count or revenue level at which infrastructure costs are covered |
| **Failure Mode** | A specific way in which a system component can fail, characterized by trigger, blast radius, and recovery path |
| **Blast Radius** | The scope of system impact when a failure occurs — from a single request to total outage |
| **Incident Simulation** | Controlled exercise to verify system behavior under failure conditions (a.k.a. game day, chaos engineering lite) |
| **North Star** | An aspirational but concrete technical target that governs all architectural decisions toward a shared vision |
| **Architectural Constraint** | A deliberate limitation on what the system will NOT support, eliminating entire classes of complexity |
| **Due Diligence** | Pre-investment technical assessment evaluating architecture risks, scalability ceiling, and operational maturity |
| **Pre-signed URL** | Time-limited authenticated URL for direct cloud storage access without proxying through the application server |

### B. Completed Development Phases (Historical)

<details>
<summary>Phase 1–4 completion log (26/26 items — 100%)</summary>

**Phase 1 — Foundation (7/7):** Auth, User entity, Migrations, Exception handling, Validation, File upload, Pagination

**Phase 2 — Architecture (6/6):** Service layer, Search, Indexing, Storage abstraction, Frontend hooks, Router

**Phase 3 — Production-Grade (7/7):** Dual DB provider, Docker, Redis, Serilog, Health check, Rate limiting, Thumbnails

**Phase 4 — Product Features (6/6):** Smart Collections, Tags M2M, Bulk operations, Undo/Redo, Real-time sync, RBAC
</details>

### C. Runbook

**Local development:**
```bash
# Backend (port 5027)
cd VAH.Backend && dotnet run

# Frontend (port 3000)
cd VAH.Frontend && npm install && npm run dev
```

**Docker Compose:**
```bash
docker-compose up        # All services: backend:5027, frontend:80, postgres:5432, redis:6379
```

**Database:** Auto-migrates on startup (dev only). For production, run migrations as explicit step before deployment.

**Backup:** `pg_dump` for database, `tar` for upload volumes.

**Secret rotation:** Update JWT key via environment variable, restart backend pods.

---

> *Architecture Assessment v10.0 — March 2026. Pre-Series A due diligence edition. Includes architectural constraints, failure mode analysis, incident simulation playbook, 2-year north star vision, optimistic assumption critique, and measurable codebase health governance rules — in addition to all prior strategic layers (principles, governance, domain boundaries, threat model, SLOs, observability, capacity planning, cost projections, pivot triggers). Review quarterly or before major releases. Update Source of Truth references (not hardcoded counts) when code changes.*
