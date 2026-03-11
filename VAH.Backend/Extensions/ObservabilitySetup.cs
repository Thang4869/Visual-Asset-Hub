using System.Diagnostics;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace VAH.Backend.Extensions;

/// <summary>
/// OpenTelemetry configuration — distributed tracing, metrics, and logging correlation.
/// Exports via OTLP (compatible with Jaeger, Grafana Tempo, Prometheus via OTel Collector).
/// </summary>
public static class ObservabilitySetup
{
    private const string ServiceName = "VAH.Backend";

    /// <summary>
    /// Shared <see cref="ActivitySource"/> for domain-level custom spans.
    /// Usage: <c>using var activity = Diagnostics.Source.StartActivity("ProcessAsset");</c>
    /// </summary>
    public static class Diagnostics
    {
        public static readonly ActivitySource Source = new(ServiceName);
    }

    /// <summary>
    /// Register the full observability platform: tracing + metrics + logging correlation.
    /// Facade that composes all sub-registrations into a single call for Program.cs.
    /// </summary>
    public static IServiceCollection AddObservabilityPlatform(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTracing(configuration);
        services.AddMetrics(configuration);
        services.AddLoggingCorrelation();
        return services;
    }

    /// <summary>
    /// Register OpenTelemetry distributed tracing with OTLP exporter.
    /// Configure the endpoint via <c>OpenTelemetry:OtlpEndpoint</c> in appsettings
    /// or the <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> environment variable.
    /// </summary>
    public static IServiceCollection AddTracing(this IServiceCollection services, IConfiguration configuration)
    {
        var otlpEndpoint = configuration["OpenTelemetry:OtlpEndpoint"];

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(ServiceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(ServiceName)  // custom domain spans via Diagnostics.Source
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    tracing.AddOtlpExporter(opts => opts.Endpoint = new Uri(otlpEndpoint));
                }
            });

        return services;
    }

    /// <summary>
    /// Register OpenTelemetry metrics with OTLP exporter.
    /// Configure the endpoint via <c>OpenTelemetry:OtlpEndpoint</c> in appsettings
    /// or the <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> environment variable.
    /// </summary>
    public static IServiceCollection AddMetrics(this IServiceCollection services, IConfiguration configuration)
    {
        var otlpEndpoint = configuration["OpenTelemetry:OtlpEndpoint"];

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(ServiceName))
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    metrics.AddOtlpExporter(opts => opts.Endpoint = new Uri(otlpEndpoint));
                }
            });

        return services;
    }

    /// <summary>
    /// Inject TraceId / SpanId into Serilog's log context so every log line
    /// correlates with distributed traces (Jaeger, Tempo, etc.).
    /// Requires <c>Serilog.Enrichers.Span</c> or the built-in <c>Activity</c> enricher.
    /// </summary>
    public static IServiceCollection AddLoggingCorrelation(this IServiceCollection services)
    {
        // Serilog.AspNetCore 10+ automatically enriches with TraceId/SpanId
        // when System.Diagnostics.Activity is active (OpenTelemetry sets this up).
        // This method exists as an explicit extension point — add custom enrichers here.
        return services;
    }
}
