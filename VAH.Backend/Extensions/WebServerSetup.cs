using System.Text.Json;
using System.Text.Json.Serialization;
using Asp.Versioning;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using VAH.Backend.Controllers;
using VAH.Backend.Data;
using VAH.Backend.Hubs;
using VAH.Backend.Models;

namespace VAH.Backend.Extensions;

/// <summary>
/// Web server configuration — Kestrel, HTTP API, Real-time API, Health Checks.
/// </summary>
public static class WebServerSetup
{
    /// <summary>Configure Kestrel body-size limits from <see cref="FileUploadConfig"/>.</summary>
    public static WebApplicationBuilder ConfigureKestrel(this WebApplicationBuilder builder)
    {
        var uploadConfig = builder.Configuration
            .GetSection(FileUploadConfig.SectionName)
            .Get<FileUploadConfig>() ?? new FileUploadConfig();

        builder.WebHost.ConfigureKestrel(options =>
        {
            // 2× max single-file size to allow multipart overhead
            options.Limits.MaxRequestBodySize = uploadConfig.MaxFileSizeBytes * 2;
        });
        return builder;
    }

    // ── HTTP API adapter ─────────────────────────────────────────

    /// <summary>
    /// Register the HTTP API surface: MVC controllers, API versioning,
    /// Swagger with JWT, and health check probes.
    /// </summary>
    public static IServiceCollection AddHttpApi(this IServiceCollection services)
    {
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(
                    new JsonStringEnumConverter(JsonNamingPolicy.KebabCaseLower));
            });

        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        })
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
            {
                Title = "VAH API",
                Version = "v1"
            });

            options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = Microsoft.OpenApi.SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = Microsoft.OpenApi.ParameterLocation.Header,
                Description = "Enter your JWT token"
            });

            options.AddSecurityRequirement(document => new Microsoft.OpenApi.OpenApiSecurityRequirement
            {
                {
                    new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer", document),
                    new List<string>()
                }
            });
        });

        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>("database", tags: ["ready"]);

        return services;
    }

    // ── Real-time API adapter ────────────────────────────────────

    /// <summary>
    /// Register the real-time surface: SignalR hubs.
    /// Separated from HTTP API so each transport can evolve independently.
    /// </summary>
    public static IServiceCollection AddRealtimeApi(this IServiceCollection services)
    {
        services.AddSignalR();
        return services;
    }

    /// <summary>Map health check endpoints — liveness and readiness.</summary>
    public static WebApplication MapHealthCheckEndpoints(this WebApplication app)
    {
        // Liveness: process alive, no dependency probes
        app.MapHealthChecks(RouteConstants.HealthLive, new HealthCheckOptions
        {
            Predicate = _ => false // no checks — just "is the process responding?"
        });

        // Readiness: DB and external dependencies
        app.MapHealthChecks(RouteConstants.HealthReady, new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });

        return app;
    }

    // ── Endpoint module discovery ────────────────────────────────

    /// <summary>
    /// Marker interface for feature endpoint modules.
    /// Implement this to have your module auto-discovered by <see cref="MapAutoDiscoveredEndpoints"/>.
    /// </summary>
    public interface IEndpointModule
    {
        /// <summary>Map this module's routes onto the application.</summary>
        static abstract void Map(WebApplication app);
    }

    /// <summary>
    /// Scan the executing assembly for <see cref="IEndpointModule"/> implementations
    /// and invoke their <c>Map</c> method. Supplements the explicit
    /// <see cref="MapSystemEndpoints"/> / <see cref="MapAssetEndpoints"/> calls
    /// for modules that opt into convention-based registration.
    /// </summary>
    public static WebApplication MapAutoDiscoveredEndpoints(this WebApplication app)
    {
        var moduleTypes = typeof(WebServerSetup).Assembly
            .GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false }
                     && t.GetInterfaces().Any(i => i == typeof(IEndpointModule)));

        foreach (var moduleType in moduleTypes.OrderBy(t => t.Name))
        {
            var mapMethod = moduleType.GetMethod(nameof(IEndpointModule.Map),
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            mapMethod?.Invoke(null, [app]);
        }

        return app;
    }

    // ── Explicit endpoint modules ───────────────────────────────

    /// <summary>
    /// System endpoints: Swagger UI (non-production), health probes
    /// (<c>/health/live</c> liveness, <c>/health/ready</c> readiness),
    /// and <c>/version</c> build metadata.
    /// </summary>
    public static WebApplication MapSystemEndpoints(this WebApplication app)
    {
        if (!app.Environment.IsProduction())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
        }

        app.MapHealthCheckEndpoints();

        // Build metadata — safe for production (no secrets, no config dump)
        var assembly = typeof(WebServerSetup).Assembly;
        var version = assembly.GetName().Version?.ToString() ?? "0.0.0";
        var buildDate = System.IO.File.GetLastWriteTimeUtc(assembly.Location).ToString("O");
        app.MapGet(RouteConstants.Version, () => Results.Ok(new { version, buildDate, environment = app.Environment.EnvironmentName }))
            .ExcludeFromDescription();

        return app;
    }

    /// <summary>
    /// Asset endpoints: auth/authz middleware, all <c>[ApiController]</c> routes,
    /// and the <see cref="AssetHub"/> SignalR hub at <c>/hubs/assets</c>.
    /// </summary>
    public static WebApplication MapAssetEndpoints(this WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.MapHub<AssetHub>(RouteConstants.AssetHub);
        return app;
    }
}
