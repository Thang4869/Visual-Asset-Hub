# 06_OPERATIONS — Incident Response, SLOs & Observability

> **Last Updated**: 2026-03-03  
> **Source**: Migrated from `ARCHITECTURE_REVIEW.md` §18, §19, §20, §21, §22  
> **Status**: Living Document — review quarterly

---

## 1. Failure Mode Matrix

| # | Failure | Trigger | Current Behavior | Expected Behavior | Blast Radius | Recovery |
|---|---------|---------|-----------------|-------------------|-------------|----------|
| FM1 | **PostgreSQL down** | Container crash / disk full | Unhandled → 500 | Health RED, cached reads, writes 503 | 🔴 Total | `docker-compose restart postgres` |
| FM2 | **Redis down** | Container crash | In-memory fallback (silent) | ✅ Handled — no alert | 🟡 Perf degradation | Auto-recovery |
| FM3 | **Disk full** (uploads) | Unbounded growth | Upload fails with IOException | Return 507, alert >80% disk | 🟠 Uploads blocked | Expand volume / purge |
| FM4 | **JWT key compromise** | Secret leaked | Attacker forges tokens | No detection / revocation | 🔴 Total auth bypass | Rotate key |
| FM5 | **Bad migration on startup** | Schema change fails | App does not start | Separate migration step | 🔴 Total outage | Fix migration, re-run |
| FM6 | **Memory leak in backend** | Long-running process | OOM → container killed | GC pressure alert at 80% | 🟡 Brief outage | Docker auto-restart |
| FM7 | **SignalR hub overload** | Mass concurrent ops | Backpressure not configured | Connection throttle | 🟡 Real-time delayed | Self-recovering |
| FM8 | **Concurrent writes** | Two users edit same entity | Last-write-wins → **silent data loss** | Return 409 Conflict | 🟠 Data integrity | No automated resolution |
| FM9 | **Orphaned files on disk** | Asset deleted, file remains | Storage leak | Background cleanup job | 🟢 No user impact | Manual cleanup |
| FM10 | **Cascading delete of large collection** | Delete 1000+ assets | Long transaction, potential timeout | Soft-delete + batch hard-delete | 🔴 Data loss if partial | Restore from backup |
| FM11 | **Image processing failure** | Corrupt/unsupported file | Thumbnail throws → asset saved without thumbs | Placeholder thumbnail, retry | 🟡 Visual degradation | Manual re-trigger |
| FM12 | **Network partition** | Docker bridge failure | Backend cannot reach Postgres/Redis | Circuit breaker, queue writes | 🔴 Cascading failures | Docker network restart |

---

## 2. Incident Simulation Playbooks

Run these exercises **before first production deployment** and quarterly thereafter.

### Sim 1: Database Failure Recovery
```
1. docker-compose stop postgres
2. Verify: health endpoint returns {"status": "Unhealthy"}
3. Verify: API returns 503 (NOT 500 with stack trace)
4. Verify: frontend shows degraded-mode banner
5. docker-compose start postgres
6. Verify: auto-recovery within 60s, no data corruption
7. Verify: SignalR connections re-establish
```
**Gap:** Step 3 → likely returns 500. Step 4 → no error boundary for API-down state.

### Sim 2: Disk Exhaustion
```
1. Fill uploads volume to 95%
2. Attempt 50MB upload
3. Verify: meaningful 507 error
4. Verify: non-upload operations unaffected
5. Clean up, verify uploads resume
```
**Gap:** No disk monitoring. Error surfaces as unhandled IOException.

### Sim 3: Token Revocation / Key Rotation
```
1. Login, obtain JWT
2. Rotate JWT secret (change env var)
3. Restart backend
4. API call with old token → verify 401
5. Re-login → verify works
```
**Gap:** No graceful key rotation. All sessions terminated simultaneously.

### Sim 4: Concurrent Modification
```
1. Open two browser sessions, same collection
2. Simultaneously: Tab A adds asset, Tab B renames collection
3. Verify: both succeed OR second gets 409
4. Verify: no silent data loss + SignalR update
```
**Gap:** Last-write-wins. No 409. Silent data hazard.

### Sim 5: Backup & Restore Drill
```
1. Create test data (collection + 10 assets + tags)
2. Execute pg_dump
3. Destroy database (docker-compose down -v)
4. Restore from dump
5. Verify: all data present, file references intact
6. Measure: total disaster → recovery time
```
**Gap:** ❌ **Never executed.** No documented restore procedure. RPO = ∞.

---

## 3. Missing Operational Runbooks

| Scenario | Exists? | Priority | Effort |
|----------|---------|----------|--------|
| Database restore from backup | ❌ | **P0 — existential risk** | 0.5 day |
| Secret rotation (JWT, DB password) | ❌ | P1 | 0.5 day |
| Container health degradation triage | ❌ | P2 | 0.5 day |
| Storage capacity emergency | ❌ | P2 | 0.5 day |
| Performance investigation (slow queries) | ❌ | P3 | 1 day |
| User data deletion request (GDPR) | ❌ | P3 | 1 day |
| Incident post-mortem template | ❌ | P2 | 0.5 day |

---

## 4. Performance Profile

### 4.1 Current Optimizations

| Strategy | Detail | Scope |
|----------|--------|-------|
| Redis Cache | Collection list: 5min absolute / 2min sliding TTL | Backend |
| In-Memory Fallback | Automatic when Redis unavailable | Backend |
| Thumbnail Pre-gen | sm(150) / md(400) / lg(800) WebP q80 | Backend |
| Nginx Cache | `/assets/` 1-year immutable, gzip enabled | Infra |
| DB Indexes | ~22 indexes on FK + common query patterns | Database |
| Pagination | Server-side, max 100 items/page | API |

### 4.2 Known Performance Risks

| Risk | Trigger | Mitigation |
|------|---------|------------|
| N+1 queries | Nested `.Include()` patterns | Audit + explicit loading |
| LIKE search degradation | >10K assets | PostgreSQL tsvector |
| Frontend re-render cascade | God Context mutations | Context splitting / Zustand |
| File serving bottleneck | Many concurrent downloads | CDN + object storage offload |

---

## 5. Service Level Objectives (SLOs)

Targets: 99.5% uptime, <500ms P95, <0.5% error rate.

### 5.1 Availability & Latency

| Metric | Target | Current Estimate |
|--------|--------|------------------|
| **Uptime** | 99.5% (3.65h downtime/month) | Unknown — no monitoring |
| **API P50 latency** | <100ms | ~50–80ms (single instance) |
| **API P95 latency** | <500ms | Unknown |
| **API P99 latency** | <2000ms | Unknown |
| **Upload P95** | <5s (50MB) | ~3–4s local network |
| **SignalR delivery** | <500ms from mutation | Unknown |

### 5.2 Error Budget

| Metric | Target |
|--------|--------|
| **Error rate (5xx)** | <0.5% of total requests |
| **Failed uploads** | <1% |
| **SignalR disconnects** | <5% reconnection rate per session |

### 5.3 Recovery Objectives

| Metric | Target | Current | Gap |
|--------|--------|---------|----|
| **RTO** | <30 min | ~10–15 min (docker-compose up) | ✅ Achievable |
| **RPO** | <24h | Manual pg_dump (unscheduled) | ❌ RPO = ∞ |
| **MTTR** | <1h | No runbook | 🟡 Needs incident response |

### 5.4 SLO Implementation Roadmap

1. **Stabilize:** Instrument Serilog for latency percentiles. Health monitoring.
2. **Modularize:** Automated daily backup (RPO <24h). Basic 5xx alerting.
3. **Scale:** Full SLO dashboard (Prometheus + Grafana). Error budget tracking.

---

## 6. Observability Architecture

### 6.1 Current State (Tier 0)

```
[Serilog Console+File] ─── Manual log reading
[Health Endpoint]      ─── Docker healthcheck (binary up/down)
```

**Gaps:** No metrics, no tracing, no alerting, no dashboards.

### 6.2 Target State (Tier 2)

```
┌───────────────────────────────────────────────────────────┐
│  METRICS (Prometheus-compatible)                          │
│  ─ HTTP rate, latency histogram, error rate               │
│  ─ Active SignalR connections                             │
│  ─ Upload throughput, cache hit/miss, DB query duration   │
├───────────────────────────────────────────────────────────┤
│  TRACING (OpenTelemetry → Jaeger/Zipkin)                  │
│  ─ HTTP → service → DB query spans                        │
│  ─ Correlation ID propagated                              │
├───────────────────────────────────────────────────────────┤
│  LOGGING (Serilog → structured JSON)                      │
│  ─ Existing sinks retained                                │
│  ─ Add: Seq or Elasticsearch for search                   │
│  ─ Correlation ID in every entry                          │
├───────────────────────────────────────────────────────────┤
│  ALERTING                                                 │
│  ─ 5xx >1% for 5 min → Slack                              │
│  ─ P95 >2s for 10 min → warning                           │
│  ─ Health fail ×3 → critical                              │
│  ─ Disk >80% → warning                                   │
│  ─ Redis lost → warning (cache fallback active)           │
└───────────────────────────────────────────────────────────┘
```

### 6.3 Implementation Path

| Phase | Add | Effort |
|-------|-----|--------|
| Stabilize | Serilog latency enricher + Health monitoring | 0.5 day |
| Modularize | Prometheus metrics + Grafana dashboard | 1–2 days |
| Scale | OpenTelemetry tracing + log aggregation + PagerDuty | 3–5 days |

---

## 7. Data Growth & Capacity Planning

### 7.1 Storage Projections

| Metric | 100 Users | 1,000 Users | 10,000 Users |
|--------|-----------|-------------|---------------|
| **Total assets** | 20K | 200K | 2M |
| **Image storage** | ~40 GB | ~400 GB | ~4 TB |
| **Thumbnail storage** | ~3 GB | ~30 GB | ~300 GB |
| **Database size** | ~50 MB | ~500 MB | ~5 GB |
| **Storage solution** | Local disk | Local/S3 | S3 (mandatory) |
| **Backup size** | Trivial | ~500 MB/day | ~5 GB/day |

### 7.2 Database Performance

| Scale | Assets Rows | LIKE Search | Action |
|-------|-------------|-------------|--------|
| 100 users | 20K | <50ms | No changes |
| 1K users | 200K | 200–500ms | **Add tsvector** |
| 10K users | 2M | >2s | tsvector + read replicas |

### 7.3 SignalR Projections

| Scale | Connections | Memory | Action |
|-------|-------------|--------|--------|
| 50 users | ~30 | ~1.5 MB | Single instance fine |
| 500 users | ~300 | ~15 MB | Single instance OK |
| 5K users | ~3,000 | ~150 MB | **Redis backplane** |

### 7.4 Capacity Thresholds

| Indicator | Threshold | Action |
|-----------|-----------|--------|
| Upload disk >70% | Migrate to object storage |
| Assets >100K rows | Implement tsvector |
| SignalR >500 | Add Redis backplane |
| DB >1 GB | Managed DB + automated backup |
| Writes >50/sec | Ensure PostgreSQL only |

---

> **Document End**
> Related: [RUNBOOK.md](RUNBOOK.md) · [TROUBLESHOOTING.md](TROUBLESHOOTING.md) · [SECURITY.md](../03_ARCHITECTURE/SECURITY.md)
