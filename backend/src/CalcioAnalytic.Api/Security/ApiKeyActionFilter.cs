using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace CalcioAnalytic.Api.Security;

/// <summary>
/// A global MVC action filter that enforces API-key authentication for the
/// ingestion write endpoints only. Requests whose path starts with
/// <c>/api/v1/ingestion</c> require a valid key header when
/// <see cref="ApiKeyOptions.Enabled"/> is <c>true</c>; all other requests, and
/// every request when the feature is disabled, pass through untouched. This
/// keeps read endpoints, the root document and health checks anonymous.
/// </summary>
public sealed class ApiKeyActionFilter : IAsyncActionFilter
{
    private const string ProtectedPathPrefix = "/api/v1/ingestion";

    private readonly IOptionsMonitor<ApiKeyOptions> _options;

    /// <summary>Initializes the filter with a monitor over the API-key options.</summary>
    /// <param name="options">The API-key options monitor.</param>
    public ApiKeyActionFilter(IOptionsMonitor<ApiKeyOptions> options)
    {
        _options = options;
    }

    /// <summary>
    /// Validates the API key for protected paths, short-circuiting with 401 when
    /// the key is missing or unrecognized.
    /// </summary>
    /// <param name="context">The executing action context.</param>
    /// <param name="next">The delegate to invoke the next filter or action.</param>
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var options = _options.CurrentValue;
        var path = context.HttpContext.Request.Path;

        var isProtected = path.StartsWithSegments(
            ProtectedPathPrefix,
            StringComparison.OrdinalIgnoreCase);

        if (options.Enabled && isProtected)
        {
            if (!IsAuthorized(context.HttpContext, options))
            {
                context.Result = new UnauthorizedResult();
                return;
            }
        }

        await next();
    }

    private static bool IsAuthorized(HttpContext httpContext, ApiKeyOptions options)
    {
        if (options.Keys is null || options.Keys.Length == 0)
        {
            return false;
        }

        if (!httpContext.Request.Headers.TryGetValue(options.HeaderName, out var provided))
        {
            return false;
        }

        var key = provided.ToString();
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        return options.Keys.Any(k => string.Equals(k, key, StringComparison.Ordinal));
    }
}
