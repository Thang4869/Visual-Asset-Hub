using Microsoft.AspNetCore.HttpOverrides;
using Serilog;
using VAH.Backend.Middleware;

namespace VAH.Backend.Extensions;

/// <summary>
/// Security pipeline — forwarded headers, HSTS, HTTPS redirection, security headers.
/// </summary>
public static class SecuritySetup
{
    /// <summary>
    /// Configure the security middleware pipeline in correct order:
    /// ForwardedHeaders → SecurityHeaders → HSTS → HTTPS Redirect.
    /// </summary>
    public static WebApplication UseSecurityPipeline(this WebApplication app)
    {
        // Trust reverse proxy X-Forwarded-* headers (Nginx, Cloudflare, etc.)
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });
        Log.Debug("Forwarded headers configured — X-Forwarded-For and X-Forwarded-Proto are trusted");

        // Security response headers (CSP, X-Frame-Options, etc.)
        app.UseSecurityHeaders();

        // HSTS + HTTPS redirection (non-dev only)
        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }
        app.UseHttpsRedirection();

        return app;
    }
}
