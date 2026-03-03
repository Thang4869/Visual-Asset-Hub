# 03_ARCHITECTURE — Strategic Roadmap & Growth Planning

> **Last Updated**: 2026-03-03  
> **Source**: Migrated from `ARCHITECTURE_REVIEW.md` §10, §11, §13, §23, §24, §25  
> **Status**: Living Document — review quarterly

---

## 1. Gap Analysis: Current vs. Future

### 1.1 Scalability Gaps

| Capability | Current | Needed for 100+ users | Gap |
|------------|---------|----------------------|-----|
| Database | SQLite (dev) / PostgreSQL (prod) | PostgreSQL only | SQLite cần loại bỏ khỏi production path |
| File Storage | Local disk (`wwwroot/uploads`) | Object storage (S3/Azure Blob) | IStorageService interface sẵn sàng |
| SignalR | Single instance, user groups | Redis backplane | Chưa cấu hình |
| Caching | Redis (single instance) | Redis Cluster hoặc managed cache | Đủ cho single instance |
| Search | LIKE queries | Full-text search (PostgreSQL tsvector) | Cần implement mới |

### 1.2 Reliability Gaps

| Capability | Current | Production Requirement | Gap |
|------------|---------|----------------------|-----|
| Testing | Zero | Unit + Integration minimum | **Critical gap** |
| CI/CD | None | Automated build/test/deploy | **Critical gap** |
| Monitoring | Health endpoint + Serilog | Metrics + Alerting + Tracing | Significant gap |
| Concurrency | Last-write-wins | Optimistic concurrency (RowVersion) | Medium gap |
| Backup | Manual pg_dump | Automated scheduled backup | Medium gap |
| HTTPS | HTTP only | TLS termination at proxy | Easy fix |

### 1.3 Maintainability Gaps

| Capability | Current | Sustainable Codebase | Gap |
|------------|---------|---------------------|-----|
| Frontend state | God Context (472 lines) | Focused contexts or Zustand | Refactor needed |
| App.jsx | 477 lines, ~50 destructured values | Container components | Refactor needed |
| ColorBoard.jsx | 555 lines | Extract sub-components | Low priority |
| API versioning | `/v1` prefix | Versioning strategy | Low priority now |

---

## 2. Recommended Improvements

### Priority Framework

```
         HIGH IMPACT
              │
    P1 ───────┼─────── P2
   (Do first) │  (Plan next)
              │
  LOW EFFORT ─┼─ HIGH EFFORT
              │
    P3 ───────┼─────── P4
   (Quick win)│   (Defer)
              │
         LOW IMPACT
```

### P1 — High Impact, Low-to-Medium Effort

| # | Item | Effort | Expected Outcome |
|---|------|--------|-----------------|
| 1 | **Add unit test project** (xUnit + Moq) | 2–3 days setup + ongoing | Safety net cho refactoring |
| 2 | **HTTPS enforcement** (Nginx TLS + HSTS) | 0.5 day | Secure data in transit |
| 3 | **URL sanitization** (whitelist `http/https`) | 0.5 day | Block XSS via URLs |
| 4 | **Optimistic concurrency** (RowVersion) | 1 day | Prevent silent data corruption |
| 5 | **Secret management** (env vars / Docker secrets) | 0.5 day | No credentials in source |
| 6 | **CI pipeline** (GitHub Actions) | 1 day | Automated quality gate |

### P2 — High Impact, High Effort

| # | Item | Effort | Expected Outcome |
|---|------|--------|-----------------|
| 7 | **Tách God Context** → domain contexts or Zustand | 2–3 days | Eliminate re-render cascade |
| 8 | **TanStack Query adoption** | 3–5 days | Caching, dedup, optimistic updates |
| 9 | **Integration test suite** (WebApplicationFactory) | 3–5 days | API contract verification |
| 10 | **Cloud storage** (S3/Azure via IStorageService) | 2–3 days | Unlock horizontal scaling |
| 11 | **CD pipeline** (auto-deploy staging) | 2–3 days | Eliminate manual deployment |

### P3 — Quick Wins

| # | Item | Effort | Expected Outcome |
|---|------|--------|-----------------|
| 12 | **Loại bỏ auto-migrate** cho production | 0.5 day | Safer deployments |
| 13 | **Environment-based API_URL** (VITE_API_URL) | 0.5 day | Deploy staging/prod easily |
| 14 | **N+1 query audit** | 1 day | Giảm DB round-trips |
| 15 | **Swagger cho production** (behind auth) | 0.5 day | API docs accessible |

### P4 — Defer

| # | Item | Effort | Notes |
|---|------|--------|-------|
| 16 | Full-text search (tsvector) | 3–5 days | Chỉ khi data volume lớn |
| 17 | Repository pattern extraction | 5–7 days | Chỉ khi >15 entities |
| 18 | E2E tests (Playwright) | 3–5 days | Sau unit + integration |
| 19 | CDN cho static assets | 1–2 days | Khi bandwidth bottleneck |
| 20 | SignalR Redis backplane | 1 day | Cho multi-instance |
| 21 | API versioning strategy | 1–2 days | Khi breaking changes planned |

---

## 3. Refactor Risk Analysis

| Refactor | Blast Radius | Rollback Difficulty | Prerequisite | Risk |
|----------|-------------|--------------------|--------------|------|
| **Tách God Context** | All 17 components | Medium | Unit tests | 🟡 Medium |
| **TanStack Query** | 11 hooks | Low (migrate hook-by-hook) | None | 🟢 Low |
| **Cloud storage** | IStorageService swap | Low (interface abstracted) | Integration tests | 🟢 Low |
| **Optimistic concurrency** | Entities + write endpoints + FE | Medium (DB migration + API) | Client 409 handling | 🟡 Medium |
| **Repository pattern** | All 12 services | High (cross-cutting) | Full test coverage | 🔴 High |
| **CI/CD pipeline** | Build + deploy process | Low (additive) | Reproducible Docker builds | 🟢 Low |

### Sequencing Constraints

```
Unit Tests (safety net)
    │
    ├──▶ Tách God Context (needs tests to verify)
    │       │
    │       └──▶ TanStack Query (benefits from smaller contexts)
    │
    ├──▶ Optimistic Concurrency (needs FE conflict UI)
    ├──▶ Cloud Storage (needs integration tests)
    └──▶ Repository Pattern (LAST — highest risk, needs full coverage)
```

---

## 4. Strategic Roadmap (3-Phase)

### Phase 1: Stabilize (Month 1–2)

**Mục tiêu:** Safety net và khắc phục rủi ro critical.

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

```
Week 5-6: Frontend Architecture
├── Tách AppContext → AssetContext + CollectionContext + UIContext
├── Adopt TanStack Query (start with assets)
├── Extract App.jsx → container components
└── N+1 query audit + fix

Week 7-8: Backend Hardening
├── Integration tests (WebApplicationFactory + test DB)
├── CD pipeline (auto-deploy staging)
├── Monitoring: structured metrics (Prometheus)
└── Automated backup schedule for PostgreSQL
```

**Exit Criteria:**
- [ ] No single context/component >250 lines
- [ ] TanStack Query cho ≥80% API calls
- [ ] Integration tests for critical API paths
- [ ] One-command deploy to staging
- [ ] Automated daily backup

### Phase 3: Scale (Month 5–8)

```
Week 9-12: Infrastructure
├── Cloud storage (S3/Azure via IStorageService)
├── SignalR Redis backplane
├── PostgreSQL full-text search (tsvector)
├── CDN for uploaded files
└── Multi-instance Docker Compose / Kubernetes

Week 13-16: Observability & Quality
├── Distributed tracing (OpenTelemetry)
├── Alerting (error rate, latency P95, disk usage)
├── E2E tests (Playwright, core flows)
├── Performance benchmarks
└── Load testing (k6, target 100 concurrent users)
```

**Exit Criteria:**
- [ ] Multi-instance verified
- [ ] ≥100 concurrent users sustained
- [ ] P95 latency <500ms for reads
- [ ] Alerting on anomalies
- [ ] E2E tests cover login → upload → share flow

---

## 5. Cost Projection

Monthly infrastructure estimates (AWS pricing, Azure/GCP comparable).

### 5.1 Infrastructure Cost by Scale

| Component | 100 Users | 1,000 Users | 10,000 Users |
|-----------|-----------|-------------|---------------|
| **Compute** | 1× t3.small ($15) | 2× t3.medium ($60) | 4× t3.large ($240) |
| **Database** | t3.micro RDS ($15) | db.t3.medium ($65) | db.r5.large + replica ($400) |
| **Redis** | t3.micro ($13) | t3.small ($25) | r5.large ($150) |
| **Object Storage** (S3) | 40 GB ($1) | 400 GB ($9) | 4 TB ($92) |
| **Data Transfer** | ~10 GB ($1) | ~100 GB ($9) | ~1 TB ($90) |
| **CDN** | — | Basic ($5) | Full ($50–100) |
| **Monitoring** | Free tier | Basic ($10) | Full ($50) |
| **Backup** | ~$2 | ~$15 | ~$100 |
| **Total Monthly** | **~$48** | **~$200** | **~$1,125** |
| **Per-user/month** | **$0.48** | **$0.20** | **$0.11** |

### 5.2 Break-Even Analysis (SaaS)

- At **$5/user/month**: Break-even at ~40 users
- At **$10/user/month**: Profitable from ~20 users
- At **$3/user/month**: Break-even at ~67 users

> Personnel cost dominates total cost at this scale. Infrastructure is <10% of total cost with 1 FTE developer.

### 5.3 Self-Hosted Option

| Scale | Minimum Hardware | Estimated Cost |
|-------|-----------------|----------------|
| Personal (1 user) | Raspberry Pi 4 / old laptop | ~$0 |
| Small team (5–20) | 4-core / 8GB VPS | $20–40/month |
| Organization (50–100) | 8-core / 32GB dedicated | $80–150/month |

---

## 6. Architectural Pivot Triggers

Conditions that indicate the current architecture needs fundamental restructuring.

| # | Indicator | Threshold | Current Value | Pivot Action |
|---|-----------|-----------|---------------|-------------|
| PT1 | **Entity count** | >30 entities | ~10 | Extract bounded contexts |
| PT2 | **API endpoint count** | >80 endpoints | ~47 | API gateway, split backends |
| PT3 | **Concurrent users** | >200 sustained | ≤1 | Horizontal scaling |
| PT4 | **Request throughput** | >500 req/sec | <50 | Caching + read replicas + CQRS |
| PT5 | **Team size** | >3 developers | 1 | Split into domain-owned modules |
| PT6 | **Deployment frequency** | >3x/day | ≤1x/week | Feature flags, canary deployment |
| PT7 | **Table rows** | >5M in any table | <1K | Partitioning, archival |
| PT8 | **File storage volume** | >1 TB local disk | <1 GB | **Mandatory pivot to S3** |
| PT9 | **Frontend bundle size** | >2 MB gzipped | ~200 KB | Code splitting, lazy routes |

### Decision Framework

```
┌─────────────────────┐     ┌─────────────────────┐     ┌─────────────────────┐
│  Current Monolith   │ ───▶ │  Modular Monolith  │ ───▶ │  Microservices    │
│  (VAH today)        │     │  (target Phase 2)   │     │  (if needed)        │
└─────────────────────┘     └─────────────────────┘     └─────────────────────┘
  Trigger: PT1, PT5           Trigger: PT2, PT3, PT4        Trigger: PT5 (>8),
                                                             PT6 (>3x/day)
```

**Current Assessment:** VAH is well within monolith territory. No pivot triggers are close to activation.

---

## 7. 2-Year North Star (2026–2028)

### Vision Statement

> By Q1 2028, VAH operates as a **multi-instance, horizontally scalable SaaS platform** serving ≤10,000 users with 99.9% uptime, zero-downtime deployments, and <200ms P95 API latency — while maintaining a codebase that a new developer can understand and ship to production within one week.

### Target Architecture (Q1 2028)

```
┌──────────────────────────────────────────────────────────────┐
│                    CDN (static assets + thumbnails)          │
└──────────────────────────┬───────────────────────────────────┘
                           │
┌──────────────────────────▼───────────────────────────────────┐
│              API Gateway / Load Balancer (TLS)               │
└─────┬──────────────────┬─────────────────────┬───────────────┘
      ▼                  ▼                     ▼
┌─────────────┐  ┌──────────────┐     ┌──────────────────┐
│ Backend ×N  │  │ Backend ×N   │     │ Background Worker│
│ (stateless) │  │ (stateless)  │     │ (thumbnails,     │
│             │  │              │     │  cleanup, search │
│             │  │              │     │  indexing)       │
└──────┬──────┘  └──────┬───────┘     └──────┬───────────┘
       └────────────────┴────────────────────┘
                        │
       ┌────────────────┼────────────────┐
       ▼                ▼                ▼
  PostgreSQL        Redis Cluster    Object Storage (S3)
  (managed)         (cache+backplane)
```

### Technical Milestones

| Quarter | Milestone | Success Metric |
|---------|-----------|---------------|
| Q2 2026 | **Foundation:** CI/CD + 70% test coverage + HTTPS + secrets | CI green, SonarQube pass |
| Q3 2026 | **Data Layer:** Cloud storage + PostgreSQL-only + optimistic concurrency | Horizontal scaling unblocked |
| Q4 2026 | **Frontend Modernization:** Context split + TanStack Query + code splitting | Largest component <250 LOC |
| Q1 2027 | **Observability:** Prometheus + Grafana + OpenTelemetry + alerting | MTTD <5min |
| Q2 2027 | **Multi-Instance:** Load balancer + SignalR backplane + stateless | Zero-downtime deploy |
| Q3 2027 | **Search & Performance:** tsvector + CDN | Search <100ms at 200K assets |
| Q4 2027 | **Operational Maturity:** DR drill, runbooks, on-call | RTO <15min verified |
| Q1 2028 | **Scale Validation:** Load test 10K users, pen-test | P95 <200ms, 99.9% uptime 30d |

### Anti-Goals for 2 Years

| We will NOT | Because |
|-------------|---------|
| Extract microservices | Monolith serves ≤10K users |
| Build mobile native app | Responsive web covers the use case |
| Implement CQRS everywhere | Partial CQRS sufficient |
| Support self-hosted deployment | Focus on SaaS |
| Add GraphQL or gRPC | REST sufficient at <15 entities |

### Decision Checkpoints (Kill-or-Invest Gates)

| Checkpoint | Date | Signal | If YES | If NO |
|------------|------|--------|--------|-------|
| Product-Market Fit | Q3 2026 | >50 paying users? | Invest in Scale | Pivot product |
| Scale Signal | Q1 2027 | >500 users? | Multi-instance + CDN | Single-instance, invest features |
| Team Growth | Q2 2027 | >3 developers? | Module boundaries | Keep monolith |
| Platform Play | Q4 2027 | API consumers? | Versioning, dev portal | Internal API only |
| Series B Readiness | Q1 2028 | Revenue justifies? | Full scale validation | Maintain steady state |

---

> **Document End**
> Related: [RISK_ASSESSMENT.md](RISK_ASSESSMENT.md) · [TECHNICAL_DEBT.md](../07_CHANGELOG/TECHNICAL_DEBT.md) · [SECURITY.md](SECURITY.md)
