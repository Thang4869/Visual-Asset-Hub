# 03_ARCHITECTURE — Security Posture & Threat Model

> **Last Updated**: 2026-03-08  
> **Source**: Migrated from `ARCHITECTURE_REVIEW.md` §16, §17 + `IMPLEMENTATION_GUIDE.md` §11  
> **Status**: Living Document — review semi-annually or before major releases

---

## 1. Implemented Security Controls

| Control | Implementation | Status |
|---------|---------------|--------|
| Authentication | JWT Bearer (HS256, 24h, ClockSkew=0) + ASP.NET Identity | ✅ |
| Auth Exception Semantics | `AuthContextMissingException` → 401 (distinct from generic `UnauthorizedAccessException`) | ✅ |
| Authorization | `[Authorize]` on all endpoints (except Auth, Health) | ✅ |
| Data Isolation | UserId FK on all entities, enforced at service layer | ✅ |
| RBAC | Owner/Editor/Viewer roles per collection | ✅ |
| CORS | Config-driven origins, AllowCredentials for SignalR | ✅ |
| Rate Limiting | 100 req/min general, 20 req/min upload, 60 req/min search (sliding) | ✅ |
| File Validation | Size (50MB), extension whitelist (27 types), MIME check | ✅ |
| Exception Privacy | Details only in Development environment | ✅ |
| Container Security | Non-root user, isolated volumes | ✅ |
| Password Policy | Min 6 chars, require digit + lowercase | ✅ |

---

## 2. Outstanding Security Items

| Item | Severity | Remediation |
|------|----------|-------------|
| No HTTPS in default config | 🟠 High | TLS at Nginx layer + HSTS header |
| URL scheme not validated | 🟡 Medium | Whitelist `http/https` on link asset creation |
| JWT key in appsettings | 🟡 Medium | Move to environment variable / Docker secret |
| No brute-force protection beyond rate limit | 🟢 Low | Account lockout (Identity supports this) |
| JWT refresh token not implemented | 🟢 Low | Add refresh token endpoint for long sessions |

---

## 3. Threat Model Overview (STRIDE)

STRIDE-based threat analysis for VAH's current attack surface.

### 3.1 Attack Surface Summary

| Entry Point | Protocol | Auth Required | Exposure |
|-------------|----------|---------------|----------|
| REST API (`/api/v1/*`) | HTTP(S) | Yes (JWT Bearer) | All CRUD operations, file upload/download |
| SignalR Hub (`/hubs/assets`) | WebSocket | Yes (JWT query param) | Real-time push events |
| Static files (`/assets/*`) | HTTP(S) | No | Uploaded files served via Nginx |
| Health endpoint (`/api/v1/health`) | HTTP(S) | No | System status (readiness + liveness) |
| Swagger UI (`/swagger`) | HTTP(S) | No (dev only) | API schema exposure |

### 3.2 STRIDE Analysis

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

### 3.3 Breach Scenario: Compromised JWT Key

**Scenario:** Attacker obtains the HS256 signing key from source control or environment leak.

| Step | Impact | Detection |
|------|--------|----------|
| 1. Forge valid JWT for any user | Full account takeover | None (valid signature) |
| 2. Access/modify/delete all victim's assets | Data loss, integrity breach | Audit log (not yet implemented) |
| 3. Escalate to admin if role claim is fabricated | Full system compromise | None |

**Mitigation plan:**
1. Move JWT key to Docker secret / Key Vault (blocks source control exposure)
2. Implement asymmetric signing (RS256) — private key never leaves server
3. Add token revocation list (Redis-backed) for emergency invalidation
4. Add audit logging for all write operations

### 3.4 Priority Remediation

| Priority | Threat | Fix | Effort |
|----------|--------|-----|--------|
| P1 | T4 (XSS via URLs) | URL scheme whitelist on backend | 0.5 day |
| P1 | T7 (Credential exposure) | Move secrets to env vars / Docker secrets | 0.5 day |
| P2 | T6 (Static file access) | Proxy file serving through API with auth check | 1–2 days |
| P2 | T1 (Token theft) | Reduce TTL to 1h + add refresh token + HttpOnly cookie option | 1–2 days |
| P3 | T5 (Upload abuse) | Per-user storage quota | 1 day |
| P3 | T9 (SignalR abuse) | Per-user connection limit | 0.5 day |

---

## 4. Compliance & Regulatory Statement

> Migrated from `IMPLEMENTATION_GUIDE.md` §11

### 4.1 Current Status

VAH hiện tại **không target regulated industries** và chưa có compliance requirement cụ thể.

### 4.2 Data Residency

- **Single-region deployment** (Constraint C8 trong ARCHITECTURE_REVIEW.md)
- Tất cả data (database + uploaded files) nằm trên cùng server / cloud region
- Không có geo-replication hay cross-border data transfer

### 4.3 GDPR Applicability (nếu target EU users)

| Requirement | Current Status | Gap |
|-------------|---------------|-----|
| Right to access (Art. 15) | 🟡 Partial — user can view own assets via UI | No export-all-data endpoint |
| Right to erasure (Art. 17) | ❌ Not implemented | No "delete my account + all data" flow |
| Data portability (Art. 20) | ❌ Not implemented | No bulk export in machine-readable format |
| Breach notification (Art. 33) | ❌ No process | No incident response plan |
| Privacy by design (Art. 25) | ✅ User data isolation (UserId FK on all entities) | — |
| Consent for processing | 🟡 Implicit (registration = consent) | No explicit consent flow |

### 4.4 GDPR Recommendations

Nếu VAH hướng tới SaaS cho EU users:
1. **P1:** Implement "Delete My Account" endpoint (cascade delete user + all assets + files)
2. **P2:** Implement data export (JSON/ZIP of all user assets + metadata)
3. **P3:** Privacy policy page + explicit consent checkbox at registration

---

> **Document End**
> Related: [SYSTEM_TOPOLOGY.md](SYSTEM_TOPOLOGY.md) · [RISK_ASSESSMENT.md](RISK_ASSESSMENT.md)
