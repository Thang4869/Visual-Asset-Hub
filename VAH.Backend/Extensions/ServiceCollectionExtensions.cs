using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using VAH.Backend.Configuration;
using VAH.Backend.Controllers;
using VAH.Backend.Data;
using VAH.Backend.Features.Assets.Application;
using VAH.Backend.Features.Assets.Application.Duplicate;
using VAH.Backend.Features.Assets.Infrastructure.Contexts;
using VAH.Backend.Features.Assets.Infrastructure.Files;
using VAH.Backend.Middleware;
using VAH.Backend.Models;
using VAH.Backend.Services;

namespace VAH.Backend.Extensions;

/// <summary>
/// Extension methods for organizing IServiceCollection registrations
/// into logical, named groups.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register all cross-cutting infrastructure services as a single platform layer:
    /// CORS, rate limiting, database, identity/auth, caching, and HTTP resilience.
    /// </summary>
    public static IServiceCollection AddInfrastructurePlatform(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCorsPolicy(configuration);
        services.AddRateLimitingPolicies();
        services.AddDatabase(configuration);
        services.AddIdentityAndAuth(configuration);
        services.AddCachingServices(configuration);
        services.AddHttpResilience();
        return services;
    }

    /// <summary>
    /// Register CORS with allowed origins from configuration.
    /// </summary>
    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:5173", "http://localhost:5174" };

        services.AddCors(options =>
        {
            options.AddPolicy("Frontend", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });

        return services;
    }

    /// <summary>
    /// Register rate limiting policies with IP-based partitioning.
    /// <list type="bullet">
    ///   <item><description><c>Fixed</c> — general API: 100 req/min per IP.</description></item>
    ///   <item><description><c>Upload</c> — file upload: 20 req/min per IP (strict).</description></item>
    ///   <item><description><c>Search</c> — search: 60 req/min per IP (sliding window).</description></item>
    ///   <item><description><c>Auth</c> — login/register: 10 req/min per IP (brute-force protection).</description></item>
    /// </list>
    /// </summary>
    public static IServiceCollection AddRateLimitingPolicies(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // General API — relaxed
            options.AddPolicy("Fixed", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIp(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 10
                    }));

            // Upload — medium
            options.AddPolicy("Upload", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIp(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 5
                    }));

            // Search — sliding window
            options.AddPolicy("Search", context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: GetClientIp(context),
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 60,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 6,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 5
                    }));

            // Auth (login/register) — strict brute-force protection
            options.AddPolicy("Auth", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIp(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 2
                    }));
        });

        return services;
    }

    private static string GetClientIp(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    /// <summary>
    /// Register a resilient <see cref="HttpClient"/> with Polly retry + circuit-breaker
    /// via <c>Microsoft.Extensions.Http.Resilience</c>.
    /// Any service injecting <see cref="IHttpClientFactory"/> gets resilience for free.
    /// </summary>
    private static IServiceCollection AddHttpResilience(this IServiceCollection services)
    {
        services.AddHttpClient("Resilient")
            .AddStandardResilienceHandler();
        return services;
    }

    /// <summary>
    /// Register EF Core DbContext with dual-provider support (SQLite / PostgreSQL).
    /// </summary>
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var dbProvider = configuration.GetValue<string>("DatabaseProvider") ?? "SQLite";
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

        services.AddDbContext<AppDbContext>(options =>
        {
            if (dbProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
            {
                options.UseNpgsql(connectionString);
            }
            else
            {
                options.UseSqlite(connectionString);
            }
        });

        // Expose provider name so AppDbContext can adapt SQL dialect
        services.AddSingleton(new DatabaseProviderInfo(dbProvider));

        return services;
    }

    /// <summary>
    /// Register ASP.NET Identity + JWT authentication with SignalR support.
    /// </summary>
    public static IServiceCollection AddIdentityAndAuth(this IServiceCollection services, IConfiguration configuration)
    {
        // --- ASP.NET Identity ---
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 6;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        // --- JWT Authentication ---
        var jwtSettings = configuration.GetSection("Jwt");
        var secretKey = jwtSettings["SecretKey"]
            ?? throw new InvalidOperationException("JWT SecretKey is not configured in appsettings.json.");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ClockSkew = TimeSpan.Zero
            };

            // SignalR sends JWT via query string instead of header
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                }
            };
        });

        // --- Authorization Policies ---
        services.AddAuthorizationBuilder()
            .AddPolicy(PolicyNames.RequireAssetRead, policy =>
                policy.RequireAuthenticatedUser())
            .AddPolicy(PolicyNames.RequireAssetWrite, policy =>
                policy.RequireAuthenticatedUser());

        return services;
    }

    /// <summary>
    /// Register Redis distributed cache (or in-memory fallback).
    /// </summary>
    public static IServiceCollection AddCachingServices(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
                options.InstanceName = "VAH:";
            });
            Log.Information("Redis distributed cache enabled");
        }
        else
        {
            services.AddDistributedMemoryCache(); // In-memory fallback
            Log.Information("Using in-memory distributed cache (no Redis configured)");
        }

        return services;
    }

    /// <summary>
    /// Register all application feature modules via a single facade.
    /// Each feature can be registered independently for testing or modular deployment.
    /// </summary>
    public static IServiceCollection AddFeatureModules(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCrossCuttingConcerns(configuration);
        services.AddAssetModule(configuration);
        services.AddCollectionModule();
        services.AddSearchModule();
        services.AddAuthModule();
        services.AddNotificationModule();
        return services;
    }

    // ── Cross-cutting ────────────────────────────────────────────

    private static IServiceCollection AddCrossCuttingConcerns(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<AssetService>());
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        services.AddOptions<FileUploadConfig>()
            .BindConfiguration(FileUploadConfig.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton(sp =>
            sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<FileUploadConfig>>().Value);

        return services;
    }

    // ── Asset module ─────────────────────────────────────────────

    private static IServiceCollection AddAssetModule(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AssetOptions>(configuration.GetSection(AssetOptions.SectionName));

        services.AddScoped<IStorageService, LocalStorageService>();
        services.AddScoped<IFileMapperService, FileMapperService>();
        services.AddScoped<AssetCleanupHelper>();
        services.AddScoped<IAssetDuplicateStrategy, InPlaceDuplicateStrategy>();
        services.AddScoped<IAssetDuplicateStrategy, TargetFolderDuplicateStrategy>();
        services.AddScoped<IAssetDuplicateStrategyFactory, AssetDuplicateStrategyFactory>();
        services.AddScoped<IAssetApplicationService, AssetApplicationService>();
        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<IBulkAssetService, BulkAssetService>();
        services.AddScoped<IThumbnailService, ThumbnailService>();

        return services;
    }

    // ── Collection module ────────────────────────────────────────

    private static IServiceCollection AddCollectionModule(this IServiceCollection services)
    {
        services.AddScoped<ICollectionService, CollectionService>();
        services.AddScoped<ISmartCollectionService, SmartCollectionService>();
        services.AddScoped<IPermissionService, PermissionService>();
        return services;
    }

    // ── Search module ────────────────────────────────────────────

    private static IServiceCollection AddSearchModule(this IServiceCollection services)
    {
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<ITagService, TagService>();
        return services;
    }

    // ── Auth module ──────────────────────────────────────────────

    private static IServiceCollection AddAuthModule(this IServiceCollection services)
    {
        services.AddScoped<IUserContextProvider, UserContextProvider>();
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }

    // ── Notification module ──────────────────────────────────────

    private static IServiceCollection AddNotificationModule(this IServiceCollection services)
    {
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IHealthCheckService, HealthCheckService>();
        return services;
    }
}
