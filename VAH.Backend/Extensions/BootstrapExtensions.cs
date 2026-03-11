namespace VAH.Backend.Extensions;

/// <summary>
/// Three-tier bootstrap layer — CoreHosting / Application / Web.
/// <list type="number">
///   <item><description><see cref="AddCoreHosting"/> — cross-cutting infra every service needs.</description></item>
///   <item><description><see cref="AddApplication"/> — domain feature modules.</description></item>
///   <item><description><see cref="AddWeb"/> — HTTP + real-time transport adapters.</description></item>
/// </list>
/// </summary>
public static class BootstrapExtensions
{
    // ── Builder (DI registration) ────────────────────────────────

    /// <summary>
    /// <b>Core hosting layer</b> — registers every cross-cutting service the host needs
    /// before any domain or transport code runs:
    /// <list type="bullet">
    ///   <item><description><b>Logging</b> — Serilog, configured from <c>appsettings.json</c>.</description></item>
    ///   <item><description><b>Infrastructure</b> — CORS, rate limiting, database (SQLite/PostgreSQL), Identity + JWT, Redis/memory cache.</description></item>
    ///   <item><description><b>Observability</b> — OpenTelemetry tracing + metrics (OTLP exporter) + logging correlation (TraceId/SpanId).</description></item>
    ///   <item><description><b>Startup initializers</b> — DB migrations (dev), seeding, cache warm-up.</description></item>
    /// </list>
    /// </summary>
    public static WebApplicationBuilder AddCoreHosting(this WebApplicationBuilder builder)
    {
        builder.AddSerilog();
        builder.Services.AddInfrastructurePlatform(builder.Configuration);
        builder.Services.AddObservabilityPlatform(builder.Configuration);
        builder.Services.AddStartupInitializers(builder.Environment);
        return builder;
    }

    /// <summary>
    /// <b>Application layer</b> — domain feature modules registered via DI:
    /// assets, collections, search, auth, notifications.
    /// See <see cref="ServiceCollectionExtensions.AddFeatureModules"/> for module breakdown.
    /// </summary>
    public static WebApplicationBuilder AddApplication(this WebApplicationBuilder builder)
    {
        builder.Services.AddFeatureModules(builder.Configuration);
        return builder;
    }

    /// <summary>
    /// <b>Web layer</b> — transport adapters:
    /// <list type="bullet">
    ///   <item><description><see cref="WebServerSetup.AddHttpApi"/> — MVC, API versioning, Swagger + JWT, health probes (<c>/health/live</c>, <c>/health/ready</c>).</description></item>
    ///   <item><description><see cref="WebServerSetup.AddRealtimeApi"/> — SignalR hubs.</description></item>
    /// </list>
    /// </summary>
    public static WebApplicationBuilder AddWeb(this WebApplicationBuilder builder)
    {
        builder.ConfigureKestrel();
        builder.Services.AddHttpApi();
        builder.Services.AddRealtimeApi();
        return builder;
    }

    // ── App (runtime middleware) ─────────────────────────────────

    /// <summary>
    /// Runtime middleware pipeline in the order ASP.NET Core requires.
    /// Each step must precede the next — reordering will break behaviour.
    /// <list type="number">
    ///   <item><description><b>Security</b> — forwarded headers, security response headers (CSP, X-Frame-Options), HSTS, HTTPS redirect.</description></item>
    ///   <item><description><b>Exception handling</b> — global RFC 7807 ProblemDetails handler.</description></item>
    ///   <item><description><b>CORS</b> — must run before any response-generating middleware.</description></item>
    ///   <item><description><b>Observability</b> — Serilog HTTP request logging (captures status code + elapsed).</description></item>
    ///   <item><description><b>Rate limiting</b> — IP-partitioned: Fixed (100/min), Upload (20/min), Search (60/min), Auth (10/min).</description></item>
    ///   <item><description><b>Static files</b> — serve wwwroot assets before hitting MVC pipeline.</description></item>
    /// </list>
    /// </summary>
    public static WebApplication UseCoreHostingPipeline(this WebApplication app)
    {
        app.UseSecurityPipeline();   // 1. Security headers, HSTS, HTTPS
        app.UseExceptionHandler();   // 2. Global error → ProblemDetails
        app.UseCors("Frontend");     // 3. CORS (before response body)
        app.UseSerilogLogging();     // 4. HTTP request logging
        app.UseRateLimiter();        // 5. Rate limiting
        app.UseStaticFiles();        // 6. wwwroot static assets
        return app;
    }
}
