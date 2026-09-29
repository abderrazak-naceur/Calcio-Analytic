using System.Diagnostics;
using System.Diagnostics.Metrics;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CalcioAnalytic.Api.Observability;

/// <summary>
/// Shared telemetry primitives for the Calcio-Analytic API. Application code can
/// use these to emit custom spans and metrics that flow through the configured
/// OpenTelemetry pipeline.
/// </summary>
public static class Telemetry
{
    /// <summary>Logical name shared by the <see cref="ActivitySource"/> and <see cref="Meter"/>.</summary>
    public const string Name = "CalcioAnalytic";

    /// <summary>Service version reported on the OpenTelemetry resource.</summary>
    public const string Version = "0.1.0";

    /// <summary>Custom activity source for application-emitted spans.</summary>
    public static readonly ActivitySource ActivitySource = new(Name, Version);

    /// <summary>Custom meter for application-emitted metrics.</summary>
    public static readonly Meter Meter = new(Name, Version);
}

/// <summary>
/// Extension methods that wire OpenTelemetry tracing and metrics into the
/// dependency-injection container.
/// </summary>
public static class OpenTelemetryExtensions
{
    /// <summary>Resource <c>service.name</c> reported to observability backends.</summary>
    private const string ServiceName = "calcio-analytic-api";

    /// <summary>
    /// Registers OpenTelemetry tracing and metrics for the API.
    /// </summary>
    /// <remarks>
    /// An OTLP exporter is only attached when an endpoint is configured via
    /// <c>OpenTelemetry:OtlpEndpoint</c> or the <c>OTEL_EXPORTER_OTLP_ENDPOINT</c>
    /// environment variable. With no endpoint configured the SDK still runs, but
    /// nothing is exported, so the application works normally without a running
    /// collector.
    /// </remarks>
    /// <param name="services">The service collection to add telemetry to.</param>
    /// <param name="config">Application configuration used to resolve the OTLP endpoint.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddCalcioObservability(
        this IServiceCollection services,
        IConfiguration config)
    {
        var otlpEndpoint = ResolveOtlpEndpoint(config);
        var hasOtlpEndpoint = !string.IsNullOrWhiteSpace(otlpEndpoint);

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(
                serviceName: ServiceName,
                serviceVersion: Telemetry.Version))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(Telemetry.Name)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                if (hasOtlpEndpoint)
                {
                    tracing.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint!));
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter(Telemetry.Name)
                    .AddAspNetCoreInstrumentation()
                    .AddRuntimeInstrumentation();

                if (hasOtlpEndpoint)
                {
                    metrics.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint!));
                }
            });

        return services;
    }

    /// <summary>
    /// Resolves the OTLP endpoint from configuration, falling back to the
    /// standard <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> environment variable.
    /// </summary>
    private static string? ResolveOtlpEndpoint(IConfiguration config)
    {
        var fromConfig = config["OpenTelemetry:OtlpEndpoint"];
        if (!string.IsNullOrWhiteSpace(fromConfig))
        {
            return fromConfig;
        }

        var fromEnv = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
        return string.IsNullOrWhiteSpace(fromEnv) ? null : fromEnv;
    }
}
