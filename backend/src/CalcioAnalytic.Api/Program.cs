using System.Diagnostics;
using CalcioAnalytic.Api.Middleware;
using CalcioAnalytic.Application;
using CalcioAnalytic.Infrastructure;
using CalcioAnalytic.Ingestion;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// OpenAPI (ASP.NET Core first-party, OpenAPI 3.1).
builder.Services.AddOpenApi();

// Application, Infrastructure (EF Core / PostgreSQL) and Ingestion wiring.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddIngestion();

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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();

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
