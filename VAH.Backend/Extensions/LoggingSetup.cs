using Serilog;
using Serilog.Events;

namespace VAH.Backend.Extensions;

/// <summary>
/// Logging configuration — Serilog bootstrap and HTTP request logging.
/// </summary>
public static class LoggingSetup
{
    /// <summary>
    /// Configure Serilog as the logging provider, reading from appsettings.json.
    /// DevOps can change log levels/sinks without rebuilding.
    /// </summary>
    public static WebApplicationBuilder AddSerilog(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((ctx, lc) =>
        {
            lc.ReadFrom.Configuration(ctx.Configuration);
        });
        return builder;
    }

    /// <summary>
    /// Add Serilog HTTP request logging with adaptive log levels.
    /// </summary>
    public static WebApplication UseSerilogLogging(this WebApplication app)
    {
        app.UseSerilogRequestLogging(opts =>
        {
            opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.000}ms";
            opts.GetLevel = (ctx, elapsed, ex) =>
                ex != null ? LogEventLevel.Error
                : ctx.Response.StatusCode >= 500 ? LogEventLevel.Error
                : elapsed > 3000 ? LogEventLevel.Warning
                : LogEventLevel.Information;
        });
        return app;
    }
}
