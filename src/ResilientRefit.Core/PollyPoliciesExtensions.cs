using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Refit;
using ResilientRefit.Core.Models;
using System;

namespace ResilientRefit.Core
{
    public static class PollyPoliciesExtensions
    {
        /// <summary>
        /// Configures a Refit client with Polly policies.
        /// </summary>
        /// <typeparam name="TClient">The type of the Refit client.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="sectionName">The configuration section name.</param>
        public static void ConfigureRefitClient<TClient>(this IServiceCollection services, IConfiguration configuration, string sectionName) where TClient : class
        {
            // Validate input parameters
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            if (string.IsNullOrEmpty(sectionName)) throw new ArgumentException("Section name cannot be null or empty", nameof(sectionName));

            // Configure Polly policies
            services.ConfigurePollyPolicies<TClient>(configuration, sectionName);

            // Retrieve the base URL from the configuration
            var baseUrl = configuration[$"{sectionName}:BaseUrl"];
            if (string.IsNullOrEmpty(baseUrl)) throw new InvalidOperationException($"Base URL for section '{sectionName}' is not configured.");

            // Configure the Refit client with the base URL and Polly policies
            services.AddRefitClient<TClient>()
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(baseUrl))
                .AddPolicyHandlerFromRegistry($"{typeof(TClient).Name}:CombinedPolicy");
        }

        /// <summary>
        /// Configures Polly policies for a Refit client.
        /// </summary>
        /// <typeparam name="TClient">The type of the Refit client.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="sectionName">The configuration section name.</param>
        private static void ConfigurePollyPolicies<TClient>(this IServiceCollection services, IConfiguration configuration, string sectionName) where TClient : class
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

            // Resolve ILogger<PolicyBuilder> from the service collection
            var serviceProvider = services.BuildServiceProvider();
            var logger = serviceProvider.GetRequiredService<ILogger<PolicyBuilder>>();

            // Build the policy pipeline
            var policy = new PolicyBuilder(logger)
                // Circuit Breaker Policy: Monitors the number of consecutive failures and opens the circuit if the threshold is reached
                .WithCircuitBreakerPolicy(resiliencySettings.CircuitBreakerPolicy.Count, TimeSpan.FromSeconds(resiliencySettings.CircuitBreakerPolicy.Duration))
                // Retry Policy: Retries the request a specified number of times with a delay between each attempt
                .WithRetryPolicy(resiliencySettings.RetryPolicy.Count, resiliencySettings.RetryPolicy.Delay, resiliencySettings.RetryPolicy.Jitter)
                // Timeout Policy: Enforces a maximum duration for each request
                .WithTimeoutPolicy(TimeSpan.FromSeconds(resiliencySettings.TimeoutPolicy.Duration))
                .Build();

            // Add the combined policy to the policy registry
            policyRegistry.Add($"{typeof(TClient).Name}:CombinedPolicy", policy);
        }
    }
}
