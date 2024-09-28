using Microsoft.Extensions.Logging;
using Polly;

namespace PolicyBuilder.NET;

public class ResiliencyPolicyBuilder
{
    private readonly ILogger<ResiliencyPolicyBuilder> _logger;
    private readonly List<IAsyncPolicy<HttpResponseMessage>> _policies = new();

    public ResiliencyPolicyBuilder(ILogger<ResiliencyPolicyBuilder> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ResiliencyPolicyBuilder WithCircuitBreakerPolicy(int failureThreshold, TimeSpan durationOfBreak)
    {
        if (failureThreshold <= 0)
            throw new ArgumentOutOfRangeException(nameof(failureThreshold), "failureThreshold must be greater than 0.");
        if (durationOfBreak <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(durationOfBreak), "durationOfBreak must be greater than 0.");

        var policy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .CircuitBreakerAsync(failureThreshold, durationOfBreak);

        _policies.Add(policy);
        return this;
    }

    public ResiliencyPolicyBuilder WithRetryPolicy(int retryCount, int delay, int jitter)
    {
        if (retryCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(retryCount), "retryCount must be greater than 0.");
        if (delay <= 0)
            throw new ArgumentOutOfRangeException(nameof(delay), "delay must be greater than 0.");
        if (jitter < 0)
            throw new ArgumentOutOfRangeException(nameof(jitter), "jitter must be greater than or equal to 0.");

        var policy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .WaitAndRetryAsync(retryCount, retryAttempt =>
                TimeSpan.FromSeconds(delay) + TimeSpan.FromMilliseconds(jitter));

        _policies.Add(policy);
        return this;
    }

    public ResiliencyPolicyBuilder WithTimeoutPolicy(TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), "timeout must be greater than 0.");

        var policy = Policy.TimeoutAsync<HttpResponseMessage>(timeout);
        _logger.LogInformation("Timeout policy added with timeout: {Timeout}", timeout);

        _policies.Add(policy);
        return this;
    }

    public IAsyncPolicy<HttpResponseMessage> Build()
    {
        if (_policies.Count == 0)
            throw new InvalidOperationException("No policies have been added to the builder.");

        _logger.LogInformation("Building policy with {PolicyCount} policies", _policies.Count);

        return _policies.Count == 1 ? _policies[0] : Policy.WrapAsync(_policies.ToArray());
    }
}