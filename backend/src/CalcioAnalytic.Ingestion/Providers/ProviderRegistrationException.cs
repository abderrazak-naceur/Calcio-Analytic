namespace CalcioAnalytic.Ingestion.Providers;

/// <summary>
/// Thrown when a provider adapter cannot be resolved: the provider code is
/// unknown, or a matching adapter does not implement the requested capability,
/// or a duplicate registration was detected.
/// </summary>
public sealed class ProviderRegistrationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderRegistrationException"/> class.
    /// </summary>
    /// <param name="message">A message describing the registration or resolution failure.</param>
    public ProviderRegistrationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderRegistrationException"/> class.
    /// </summary>
    /// <param name="message">A message describing the registration or resolution failure.</param>
    /// <param name="innerException">The underlying cause.</param>
    public ProviderRegistrationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
