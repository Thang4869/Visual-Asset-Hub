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
    /// Register rate limiting policies (Fixed + Upload).
    /// </summary>
    public static IServiceCollection AddRateLimitingPolicies(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddFixedWindowLimiter("Fixed", opt =>
            {
                opt.PermitLimit = 100;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 10;
            });

            options.AddFixedWindowLimiter("Upload", opt =>
            {
                opt.PermitLimit = 20;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 5;
            });
        });

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
    /// Register all application-specific services (storage, assets, collections, etc.).
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.Configure<AssetOptions>(configuration.GetSection(AssetOptions.SectionName));

        // ── MediatR (CQRS pipeline) ──
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<AssetService>());

        services.AddScoped<IUserContextProvider, UserContextProvider>();
        services.AddScoped<IFileMapperService, FileMapperService>();
        services.AddScoped<IAssetDuplicateStrategy, InPlaceDuplicateStrategy>();
        services.AddScoped<IAssetDuplicateStrategy, TargetFolderDuplicateStrategy>();
        services.AddScoped<IAssetDuplicateStrategyFactory, AssetDuplicateStrategyFactory>();
        services.AddScoped<IAssetApplicationService, AssetApplicationService>();

        // ── Global Exception Handler (RFC 7807) ──
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        services.AddSingleton(new FileUploadConfig());
        services.AddScoped<IStorageService, LocalStorageService>();
        services.AddScoped<AssetCleanupHelper>();
        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<IBulkAssetService, BulkAssetService>();
        services.AddScoped<ICollectionService, CollectionService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IThumbnailService, ThumbnailService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<ISmartCollectionService, SmartCollectionService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IHealthCheckService, HealthCheckService>();

        return services;
    }
}
