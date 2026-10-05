using System.Diagnostics;
using System.Threading.RateLimiting;
using CalcioAnalytic.Analytics;
using CalcioAnalytic.Api.Middleware;
using CalcioAnalytic.Api.Observability;
using CalcioAnalytic.Api.Security;
using CalcioAnalytic.Application;
using CalcioAnalytic.Infrastructure;
using CalcioAnalytic.Ingestion;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// API-key authentication options (disabled by default; see appsettings.json).
builder.Services.Configure<ApiKeyOptions>(
    builder.Configuration.GetSection(ApiKeyOptions.SectionName));

// MVC controllers. Serialize enums as their string names to keep the JSON
// contract stable and human-readable. A global action filter enforces API-key
// auth on the ingestion write endpoints only (and only when enabled).
builder.Services
    .AddControllers(options =>
    {
        options.Filters.Add<ApiKeyActionFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Fixed-window rate limiting. The limit is intentionally generous so it never
// trips during normal use or integration tests; health checks are excluded.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var path = context.Request.Path;
        if (path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase))
        {
            return RateLimitPartition.GetNoLimiter("health");
        }

        var partitionKey = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromSeconds(10),
            QueueLimit = 0,
        });
    });
});

// OpenAPI (ASP.NET Core first-party, OpenAPI 3.1).
builder.Services.AddOpenApi();

// Application, Infrastructure (EF Core / PostgreSQL) and Ingestion wiring.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddIngestion();
builder.Services.AddMockProvider();
builder.Services.AddSportmonksProvider();
builder.Services.AddAnalytics();

// OpenTelemetry tracing and metrics. Exports via OTLP only when an endpoint is
// configured; otherwise the SDK runs without an exporter and the app is unaffected.
builder.Services.AddCalcioObservability(builder.Configuration);

// Health checks. Liveness is tag "live"; readiness includes dependencies.
var healthChecks = builder.Services.AddHealthChecks();

var postgresConnectionString = builder.Configuration.GetConnectionString("Postgres");
if (!string.IsNullOrWhiteSpace(postgresConnectionString))
{
    healthChecks.AddNpgSql(
        postgresConnectionString,
        name: "postgres",
        tags: new[] { "ready" });
}

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:5173", "http://localhost:3000" };
        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCorrelationId();
app.UseSecurityHeaders();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseRateLimiter();

// Liveness: process is up. Readiness: dependencies reachable.
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = HealthResponseWriter.WriteResponse,
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = HealthResponseWriter.WriteResponse,
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = HealthResponseWriter.WriteResponse,
});

app.MapGet("/", () => Results.Ok(new
{
    name = "Calcio-Analytic API",
    version = "0.1.0",
    status = "ok",
}));

app.MapControllers();

app.Run();

/// <summary>Writes health check results as a small JSON document.</summary>
internal static class HealthResponseWriter
{
    public static Task WriteResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        var payload = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
            }),
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
        };
        return context.Response.WriteAsJsonAsync(payload);
    }
}

/// <summary>Marker to allow WebApplicationFactory-based integration tests.</summary>
public partial class Program;
