using Microsoft.Extensions.DependencyInjection;

namespace CalcioAnalytic.Application;

/// <summary>
/// Composition root helpers for the Application layer. Provides a single
/// registration entry point for application-level services such as handlers
/// and validators.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the Application layer services into the DI container.
    /// Currently a no-op placeholder that returns the collection so callers
    /// can chain registrations; future handlers and validators are wired here.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}
