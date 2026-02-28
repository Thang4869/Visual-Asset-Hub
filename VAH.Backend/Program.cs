using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using VAH.Backend.Data;
using VAH.Backend.Extensions;
using VAH.Backend.Hubs;
using VAH.Backend.Middleware;

// ---- Bootstrap Serilog (early, before host build) ----
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}{NewLine}  {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/vah-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] ({SourceContext}) {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
Log.Information("Starting VAH Backend...");

var builder = WebApplication.CreateBuilder(args);

// ---- Use Serilog as the logging provider ----
builder.Host.UseSerilog();

// --- Infrastructure services (grouped via extension methods) ---
builder.Services.AddCorsPolicy(builder.Configuration);
builder.Services.AddRateLimitingPolicies();
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddIdentityAndAuth(builder.Configuration);
builder.Services.AddCachingServices(builder.Configuration);
builder.Services.AddApplicationServices();

// --- Kestrel limits ---
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 100 * 1024 * 1024; // 100 MB
});

// --- MVC / Swagger ---
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(JsonNamingPolicy.KebabCaseLower));
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- SignalR ---
builder.Services.AddSignalR();

// ============================================================
var app = builder.Build();

// --- Global exception handler (RFC 7807 via IExceptionHandler) ---
app.UseExceptionHandler();

// --- CORS ---
app.UseCors("Frontend");

// --- Serilog HTTP request logging ---
app.UseSerilogRequestLogging(opts =>
{
    opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.000}ms";
    opts.GetLevel = (ctx, elapsed, ex) =>
        ex != null ? LogEventLevel.Error
        : ctx.Response.StatusCode >= 500 ? LogEventLevel.Error
        : elapsed > 3000 ? LogEventLevel.Warning
        : LogEventLevel.Information;
});

// --- Rate Limiting ---
app.UseRateLimiter();

// --- Static files ---
app.UseStaticFiles();

// --- Swagger ---
app.UseSwagger();
app.UseSwaggerUI();

// --- Redirect root to Swagger ---
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<AssetHub>("/hubs/assets");

// --- Initialize database (auto-migrate) ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    // --- Fix existing assets with wrong ContentType discriminator ---
    // Assets created before the factory fix had ContentType='file' for all subtypes.
    // Fix based on collection type and asset characteristics.
    db.Database.ExecuteSqlRaw(@"
        UPDATE Assets SET ContentType = 'image'
        WHERE ContentType = 'file' AND FilePath LIKE '/uploads/%'
          AND IsFolder = 0 AND CollectionId IN (SELECT Id FROM Collections WHERE Type = 'image');

        UPDATE Assets SET ContentType = 'link'
        WHERE ContentType = 'file' AND FilePath LIKE 'http%'
          AND IsFolder = 0;

        UPDATE Assets SET ContentType = 'color'
        WHERE ContentType = 'file' AND FilePath LIKE '#%'
          AND IsFolder = 0 AND CollectionId IN (SELECT Id FROM Collections WHERE Type = 'color');

        UPDATE Assets SET ContentType = 'color-group'
        WHERE ContentType = 'file' AND FilePath = ''
          AND IsFolder = 0 AND CollectionId IN (SELECT Id FROM Collections WHERE Type = 'color');

        UPDATE Assets SET ContentType = 'folder'
        WHERE ContentType = 'file' AND IsFolder = 1;
    ");
}

app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "VAH Backend terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>Exposes the configured DB provider name so AppDbContext can adapt SQL dialect.</summary>
public record DatabaseProviderInfo(string ProviderName)
{
    public bool IsPostgreSql => ProviderName.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase);
    public bool IsSqlite => !IsPostgreSql;
}