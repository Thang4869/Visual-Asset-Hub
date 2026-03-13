namespace VAH.Backend.Middleware;

/// <summary>
/// Adds production-grade security response headers to every HTTP response.
/// <list type="bullet">
///   <item><description><c>Content-Security-Policy</c> — restricts resource origins (API-only: default-src 'none').</description></item>
///   <item><description><c>X-Content-Type-Options: nosniff</c> — prevents MIME-type sniffing.</description></item>
///   <item><description><c>X-Frame-Options: DENY</c> — prevents clickjacking via iframes.</description></item>
///   <item><description><c>Referrer-Policy: strict-origin-when-cross-origin</c> — limits referrer leakage.</description></item>
///   <item><description><c>Permissions-Policy</c> — disables unused browser features.</description></item>
///   <item><description><c>X-XSS-Protection: 0</c> — disables legacy XSS auditor (CSP is preferred).</description></item>
/// </list>
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public Task Invoke(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers["Content-Security-Policy"] =
            "default-src 'self'; style-src 'self' 'unsafe-inline'; script-src 'self' 'unsafe-inline'; img-src 'self' data:; connect-src 'self'; frame-ancestors 'none'";
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        headers["X-XSS-Protection"] = "0";

        return next(context);
    }
}

public static class SecurityHeadersExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        => app.UseMiddleware<SecurityHeadersMiddleware>();
}
