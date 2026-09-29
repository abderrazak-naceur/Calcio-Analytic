namespace CalcioAnalytic.Api.Security;

/// <summary>
/// Adds a small, conservative set of security-related response headers suitable
/// for a JSON API that never returns HTML. The Content-Security-Policy is a
/// hardened, minimal policy (<c>default-src 'none'; frame-ancestors 'none'</c>)
/// because the API serves no scripts, styles, or framed content.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>Initializes the middleware with the next delegate in the pipeline.</summary>
    /// <param name="next">The next request delegate.</param>
    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>Attaches the security headers before the response is sent.</summary>
    /// <param name="context">The current HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
            return Task.CompletedTask;
        });

        await _next(context);
    }
}

/// <summary>Extension methods for registering <see cref="SecurityHeadersMiddleware"/>.</summary>
public static class SecurityHeadersMiddlewareExtensions
{
    /// <summary>Adds <see cref="SecurityHeadersMiddleware"/> to the request pipeline.</summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The same application builder for chaining.</returns>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        => app.UseMiddleware<SecurityHeadersMiddleware>();
}
