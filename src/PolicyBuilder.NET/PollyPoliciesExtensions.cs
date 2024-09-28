using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PolicyBuilder.NET.Models;

namespace PolicyBuilder.NET;

public static class PollyPoliciesExtensions
{
    /// <summary>
    /// Configures Polly policies for a Refit client.
    /// </summary>
    /// <typeparam name="TClient">The type of the Refit client.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="sectionName">The configuration section name.</param>
    /// <returns>The name of the combined policy added to the policy registry.</returns>
    public static string ConfigurePollyPolicies<TClient>(this IServiceCollection services, IConfiguration configuration, string sectionName) where TClient : class
    {
        // Validate input parameters
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        if (string.IsNullOrEmpty(sectionName)) throw new ArgumentException("Section name cannot be null or empty", nameof(sectionName));

        // Add a policy registry to the service collection
        var policyRegistry = services.AddPolicyRegistry();

        // Retrieve resiliency settings from the configuration
        var resiliencySettings = configuration.GetSection($"{sectionName}:ResiliencyPolicies").Get<ResiliencySettings>();
        if (resiliencySettings == null) throw new InvalidOperationException($"Resiliency settings for section '{sectionName}' are not configured.");

        // Resolve ILogger<ResiliencyPolicyBuilder> from the service collection
        var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<global::PolicyBuilder.NET.ResiliencyPolicyBuilder>>();

        // Build the policy pipeline using the retrieved resiliency settings
        var policy = new global::PolicyBuilder.NET.ResiliencyPolicyBuilder(logger)
            // Circuit Breaker Policy: Monitors the number of consecutive failures and opens the circuit if the threshold is reached
            .WithCircuitBreakerPolicy(resiliencySettings.CircuitBreakerPolicy.Count, TimeSpan.FromSeconds(resiliencySettings.CircuitBreakerPolicy.Duration))
            // Retry Policy: Retries the request a specified number of times with a delay between each attempt
            .WithRetryPolicy(resiliencySettings.RetryPolicy.Count, resiliencySettings.RetryPolicy.Delay, resiliencySettings.RetryPolicy.Jitter)
            // Timeout Policy: Enforces a maximum duration for each request
            .WithTimeoutPolicy(TimeSpan.FromSeconds(resiliencySettings.TimeoutPolicy.Duration))
            .Build();

        // Generate a unique policy name based on the client type
        var policyName = $"{typeof(TClient).Name}:CombinedPolicy";

        // Add the combined policy to the policy registry
        policyRegistry.Add(policyName, policy);

        // Return the name of the combined policy
        return policyName;
    }
}