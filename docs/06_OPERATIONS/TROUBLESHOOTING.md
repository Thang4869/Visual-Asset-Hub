# TROUBLESHOOTING — Common Issues & Solutions

> **Last Updated**: 2026-03-02

---

## §1 — Backend Issues

### 1.1 Database Migration Fails

**Symptom**: `dotnet ef database update` throws `SqlException` or `SqliteException`

**Solutions:**
1. **Check provider**: Ensure `DatabaseProvider` in appsettings matches your target DB
2. **Pending migrations**: Run `dotnet ef migrations list` to see unapplied migrations
3. **Reset**: Delete `vah.db` (SQLite) or `docker compose down -v` (PostgreSQL)
4. **Discriminator mismatch**: The startup SQL fix handles this — ensure `Program.cs` auto-migration runs

### 1.2 JWT Authentication Fails (401)

**Symptom**: All API calls return 401 Unauthorized

**Solutions:**
1. **Check token**: Decode at https://jwt.io — verify expiry, issuer, audience
2. **Config mismatch**: `Jwt:SecretKey`, `Jwt:Issuer`, `Jwt:Audience` must match between token generation and validation
3. **ClockSkew = Zero**: Server time must be accurate — no tolerance for clock drift
4. **SignalR**: Ensure token sent via `?access_token=` query param, not header

### 1.3 File Upload Fails

**Symptom**: 413 Payload Too Large or 400 Bad Request on upload

**Solutions:**
1. **Kestrel limit**: Program.cs sets 100 MB — check if file exceeds this
2. **FileUploadConfig**: Max 50 MB per file, 20 files per request
3. **Rate limit**: Upload policy allows 20/min — check if throttled
4. **Extension**: Verify file extension is in `FileUploadConfig.AllowedExtensions`
5. **Disk space**: Check `wwwroot/uploads/` has sufficient storage

### 1.4 SignalR Connection Fails

**Symptom**: Real-time updates not working, console shows WebSocket errors

**Solutions:**
1. **CORS**: Ensure `AllowCredentials()` is set (required for SignalR)
2. **Nginx proxy**: WebSocket upgrade headers must be forwarded
3. **Auth**: Token must be valid when connection is established
4. **Reconnect**: `withAutomaticReconnect()` should be configured in frontend

### 1.5 Thumbnail Generation Fails

**Symptom**: Images upload but thumbnails are null

**Solutions:**
1. **ImageSharp**: Verify SixLabors.ImageSharp 3.1.12 is installed
2. **Permissions**: `wwwroot/uploads/thumbs/` directory must be writable
3. **Format**: ImageSharp supports JPEG, PNG, GIF, WebP, BMP — SVG not supported
4. **Memory**: Large images may OOM — check Kestrel memory limits

## §2 — Frontend Issues

### 2.1 CORS Errors

**Symptom**: Browser console shows `Access-Control-Allow-Origin` errors

**Solutions:**
1. **Origin**: Frontend URL must be in `Cors:AllowedOrigins` (default: `localhost:5173,5174`)
2. **Credentials**: `AllowCredentials()` required for SignalR — incompatible with `AllowAnyOrigin()`
3. **Docker**: When using Docker, frontend is at port 3000 — ensure it's in allowed origins

### 2.2 API Calls Return Network Error

**Symptom**: Axios throws `ERR_NETWORK` or `ERR_CONNECTION_REFUSED`

**Solutions:**
1. **Backend running**: Verify backend is up at `http://localhost:5027/api/v1/health`
2. **Vite proxy**: Check `vite.config.js` proxy settings for API forwarding
3. **Docker networking**: Services communicate by container name, not `localhost`

### 2.3 State Not Updating After Operations

**Symptom**: UI doesn't reflect changes after CRUD operations

**Solutions:**
1. **SignalR disconnected**: Check `useSignalR` hook connection status
2. **Missing handler**: Verify event type is registered in `AppContext.signalRHandlers`
3. **Stale closure**: Ensure `refreshItems` is in hook dependency array
4. **Cache**: Check if `IDistributedCache` is serving stale data

## §3 — Docker Issues

### 3.1 Containers Won't Start

```bash
# Check logs
docker compose logs backend

# Common fixes
docker compose down
docker compose build --no-cache
docker compose up -d
```

### 3.2 Database Connection Refused

**Symptom**: Backend fails to connect to PostgreSQL on startup

**Solutions:**
1. **Startup order**: Backend depends on PostgreSQL — Docker Compose handles this but initial startup may race
2. **Connection string**: Verify `Host=postgres` (container name, not `localhost`)
3. **Volume**: If DB volume is corrupted, `docker compose down -v` and recreate

### 3.3 Redis Connection Issues

**Symptom**: Warnings about cache in logs

**Non-critical**: Backend falls back to `DistributedMemoryCache` if Redis is unavailable. Application continues working without Redis, but without distributed caching.

---

> **Document End**
