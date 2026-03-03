# 03_ARCHITECTURE — Risk Assessment & Dependency Analysis

> **Last Updated**: 2026-03-03  
> **Source**: Migrated from `ARCHITECTURE_REVIEW.md` §2, §6, §8, §9, §26  
> **Status**: Living Document — review quarterly

---

## 1. Architectural Constraints (Deliberate Non-Support Declarations)

Intentional boundaries — what VAH will **NOT** support. Each eliminates an entire class of complexity. Violating a constraint without an ADR is an architectural breach.

| # | Constraint | Rationale | Re-evaluation Trigger |
|---|-----------|-----------|----------------------|
| C1 | **No multi-tenancy** | Single-user or team-scoped data plane. Shared infra, isolated data. | Enterprise contract requiring hard tenant isolation |
| C2 | **No offline-first / PWA** | Always-connected assumption. Local-first sync engines (CRDTs) are an order of magnitude more complex. | Mobile app requirement with unreliable connectivity |
| C3 | **No microservices before 3 developers** | Monolith is correct for team of 1. Distributed systems tax exceeds benefit. | PT5 threshold in §3.4 (Pivot Triggers) |
| C4 | **No custom auth provider / OAuth federation** | ASP.NET Identity + JWT only. No SAML, no OIDC, no social login. | Enterprise SSO requirement |
| C5 | **No real-time collaboration** | SignalR for push notifications only, not operational transform / CRDT co-editing. | Google Docs-style co-editing requirement |
| C6 | **No mobile-native app** | Web-only (responsive SPA). | >30% mobile traffic with poor responsive UX |
| C7 | **No file versioning / history** | Upload overwrites. No Git-like history for assets. Versioning triples storage cost. | Regulatory compliance requiring audit trail |
| C8 | **No multi-region deployment** | Single region. No geo-replication. | Latency >200ms to primary region for majority of users |
| C9 | **No GraphQL** | REST-only API surface. | Frontend requesting flexible queries across >10 joined entities |
| C10 | **Max 50MB per file, no chunked upload** | Hard limit. Chunked/resumable upload (tus protocol) is a significant investment. | User demand for video assets or large PSD files |

### Constraint Violation Process

1. **Identify:** Developer recognizes a feature requires violating a constraint
2. **ADR:** Write ADR documenting why the constraint no longer holds
3. **Review:** Principal engineer or tech lead approves
4. **Update:** This section updated — constraint moved to "Retired" with date and ADR reference
5. **Implement:** Proceed with implementation

---

## 2. Identified Anti-Patterns

### 2.1 God Context (Frontend)

**File:** `AppContext.js` — 472 dòng, compose **tất cả** domain hooks.

**Hệ quả:** Bất kỳ state change nào đều trigger re-render toàn bộ component tree qua context.

**Khuyến nghị:** Tách thành multiple focused contexts hoặc migrate sang Zustand với selector pattern.

### 2.2 Fat Component

**File:** `App.jsx` — 477 dòng, destructure ~50 values từ `useAppContext()`.

**Khuyến nghị:** Extract layout regions thành container components.

### 2.3 Service–DbContext Direct Coupling

**Hiện trạng:** Tất cả 12 services inject `AppDbContext` trực tiếp.

**Đánh giá:** Acceptable cho project size hiện tại (<50 entities). Sẽ trở thành nợ kỹ thuật nghiêm trọng nếu domain mở rộng.

### 2.4 Auto-Migrate on Startup

`Database.Migrate()` chạy mỗi khi application start.

**Rủi ro production:** Migration failure = app không start = downtime. Không có rollback path.

### 2.5 Manual Data Fetching (Frontend)

Pattern `useEffect` + `useState` cho mọi API call — không cache, không dedup, không stale-while-revalidate.

**Khuyến nghị:** Adopt TanStack Query (React Query).

---

## 3. Architectural Risk Matrix

| # | Risk | Severity | Likelihood | Impact | Mitigation Status |
|---|------|----------|------------|--------|-------------------|
| R1 | **Zero test coverage** | 🔴 Critical | Certain | Regression bugs vào production | ❌ No mitigation |
| R2 | **Single-instance constraint** | 🔴 High | High nếu scale | No HA, single point of failure | 🟡 Interface ready |
| R3 | **No CI/CD pipeline** | 🟠 High | Certain | Manual deploy = error-prone | ❌ No mitigation |
| R4 | **No concurrency control** | 🟠 High | Medium | Last-write-wins → silent data corruption | ❌ No mitigation |
| R5 | **God Context re-render cascade** | 🟡 Medium | Growing | UX degradation | ❌ No mitigation |
| R6 | **LIKE-based search** | 🟡 Medium | Growing | Query time tăng tuyến tính | ❌ No mitigation |
| R7 | **No HTTPS enforcement** | 🟡 Medium | Certain in prod | Credentials plaintext | ❌ No mitigation |
| R8 | **SQLite/PostgreSQL behavioral drift** | 🟡 Medium | Ongoing | Prod-only bugs | 🟡 Partially mitigated |
| R9 | **XSS via link URLs** | 🟡 Medium | Low | `javascript:` URLs saved and rendered | ❌ No mitigation |
| R10 | **No secret management** | 🟡 Medium | Certain in prod | JWT key + DB creds in appsettings | ❌ No mitigation |

### R1 Deep Dive — Zero Test Coverage

Đây là rủi ro hệ thống quan trọng nhất. Với 12 backend services, 11 frontend hooks, 17 components và ~47 API endpoints:
- **Mỗi code change** đều có khả năng gây regression không phát hiện được
- **Refactor** trở nên rủi ro cao vì không có safety net
- **API contract** giữa frontend/backend không được verify tự động

---

## 4. External Dependency Risk Matrix

### 4.1 Runtime Dependencies

| Dependency | Current Version | EOL / Support Window | Upgrade Risk | Action |
|------------|----------------|---------------------|--------------|--------|
| **.NET** | 9.0 (STS) | May 2026 (6 months) | 🟠 Must upgrade to .NET 10 LTS | Plan .NET 10 upgrade sprint |
| **ASP.NET Core** | 9.0 | Tied to .NET 9 STS | 🟠 Same as above | Same |
| **EF Core** | 9.0 | Tied to .NET 9 | 🟡 Migration scripts may need adjusting | Test on .NET 10 preview |
| **PostgreSQL** | 17.x | ~2029 | 🟢 Low risk | Annual minor updates |
| **Redis** | 7.x | Active | 🟢 Low risk | Monitor licensing changes |
| **React** | 19.x | Active LTS | 🟢 Low risk | Follow advisories |
| **Vite** | 7.x | Active, fast release cycle | 🟡 Breaking changes between majors | Pin major, upgrade quarterly |
| **Node.js** | (Vite runtime) | Track LTS releases | 🟢 Low risk | Use LTS only |

### 4.2 Library Risk

| Library | Purpose | Maintenance Status | Risk |
|---------|---------|-------------------|------|
| `Serilog.*` | Structured logging | ✅ Active, multi-maintainer | 🟢 Low |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | JWT auth | ✅ Microsoft-maintained | 🟢 Low |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | PostgreSQL provider | ✅ Active | 🟢 Low |
| `SixLabors.ImageSharp` | Image processing | ✅ Active | 🟢 Low |
| `axios` | HTTP client (frontend) | ✅ Active | 🟢 Low |
| `react-dropzone` | File upload UI | 🟡 Moderate | 🟡 Medium — have fallback plan |
| `react-color` | Color picker | 🟡 Moderate | 🟡 Medium — lightweight, replaceable |

### 4.3 .NET 9 → 10 LTS Migration Checklist

> **Deadline:** Before .NET 9 EOL (May 2026)

- [ ] Review .NET 10 breaking changes (preview release notes)
- [ ] Validate EF Core 10 migration compatibility
- [ ] Update Dockerfile base images (`mcr.microsoft.com/dotnet/aspnet:10.0`)
- [ ] Run full test suite on .NET 10 preview
- [ ] Update CI/CD pipeline target framework
- [ ] Verify Npgsql + SQLite provider compatibility
- [ ] Performance benchmark comparison

---

## 5. Optimistic Assumption Critique

A pre-Series A due diligence review must challenge hidden assumptions. Being wrong about any of these could cause material harm.

### A1: "IStorageService abstraction makes cloud migration easy"

**Claim:** Cloud storage is a 2–3 day swap.

**Critique:** Interface guarantees signature compatibility, not behavioral compatibility:
- S3 requires pre-signed URLs with expiry — different URL lifetime semantics
- Cloud needs streaming upload — different I/O model
- S3 DELETE + DB transaction is not atomic — orphaned files risk
- S3 charges per PUT/GET — unexpected egress costs
- Existing data needs migration script

**Realistic estimate:** 5–7 days, not 2–3.

### A2: "SQLite→PostgreSQL drift is manageable"

**Critique:** This is the **most dangerous assumption**:
- `DateTime`: SQLite stores as ISO string, PostgreSQL as `timestamp with time zone`
- Case sensitivity: SQLite LIKE is case-insensitive, PostgreSQL is case-sensitive
- Transaction isolation differs (database-level vs. row-level locking)
- `DatabaseProviderInfo` only guards KNOWN differences

**Recommendation:** Remove SQLite from dev path. Use `docker compose up postgres`. **Single highest-ROI change.**

### A3: "Zero test coverage is the #1 risk"

**Critique:** Testing is the most VISIBLE risk, but actual #1 existential risk is: **no backup and no verified recovery procedure.**
- With zero tests: ship bugs (fixable)
- With zero backups: lose everything (permanent)

**Correction:** Reclassify automated backup + verified restore as **equal severity to testing**.

### A4: "Rate limiting prevents abuse"

**Critique:** 20 uploads/min × 50MB = 1 GB/minute = **1.4 TB/day**. Rate limiting doesn't prevent storage exhaustion.

**Recommendation:** Per-user storage quota + per-account login throttle.

### A5: "Docker Compose is sufficient for production"

**Critique:** Docker Compose is a dev orchestrator:
- No rolling updates → downtime on every deploy
- No resource limits by default
- No service replacement on persistent failure

**For production:** Add explicit `mem_limit`, `cpus`, external secret injection. Target Docker Swarm or managed K8s.

### A6: "RBAC is implemented"

**Critique:** RBAC is collection-level only. Missing:
- ❌ No workspace-level roles
- ❌ No permission inheritance (sub-collections)
- ❌ No individual asset permissions
- ❌ No audit trail for permission changes
- ❌ No time-limited access

### A7: "The roadmap timeline is realistic"

**Critique:** Apply 1.5–2× multiplier when feature development competes for engineering time.

| Assumption | Stated | Corrected | Impact |
|-----------|--------|-----------|--------|
| Cloud storage migration | 2–3 days | 5–7 days | Underestimated 2× |
| SQLite drift risk | Manageable | Dangerous | Remove ASAP |
| #1 risk | Zero tests | Zero backup (equally critical) | Existential risk |
| Rate limiting | Prevents abuse | Slows abuse | Storage exhaustion |
| Docker Compose | Prod-ready | Dev-grade | Downtime on deploys |
| RBAC | Implemented | Partially | Audit gap |
| Roadmap | 8 months | 12–16 months | Resource planning error |

---

> **Document End**
> Related: [SECURITY.md](SECURITY.md) · [STRATEGIC_ROADMAP.md](STRATEGIC_ROADMAP.md) · [TECHNICAL_DEBT.md](../07_CHANGELOG/TECHNICAL_DEBT.md)
