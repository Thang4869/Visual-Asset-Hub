using Serilog;
using VAH.Backend.Extensions;

// ---- Bootstrap Serilog (console-only, before host build) ----
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
Log.Information("Starting VAH Backend...");

var builder = WebApplication.CreateBuilder(args);

// ── Three-tier bootstrap ──────────────────────────────────────
builder.AddCoreHosting();   // Serilog, infra, OTEL tracing/metrics, initializers
builder.AddApplication();   // feature modules (assets, collections, search…)
builder.AddWeb();           // HTTP API + real-time API (Kestrel, MVC, SignalR)

// ════════════════════════════════════════════════════════════
var app = builder.Build();

// ── Runtime pipeline ──────────────────────────────────────────
// security → exceptions → CORS → Serilog request log → rate limiting → static files
app.UseCoreHostingPipeline();

// ── Endpoint modules (explicit for readability) ─────────────────
app.MapSystemEndpoints();    // Swagger, /health/live, /health/ready, /version
app.MapAssetEndpoints();     // api/v1/assets/*, SignalR /hubs/assets
app.MapAutoDiscoveredEndpoints(); // IEndpointModule auto-discovery

// ── Startup tasks ─────────────────────────────────────────────
await app.RunStartupInitializersAsync();
await app.RunAsync();

}
catch (Exception ex)
{
    Log.Fatal(ex, "VAH Backend terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}