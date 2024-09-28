using System.Net;
using Microsoft.Extensions.Configuration;
using Polly;
using Polly.Contrib.Simmy;
using Polly.Contrib.Simmy.Latency;
using Polly.Contrib.Simmy.Outcomes;
using Polly.Extensions.Http;

namespace PolicyBuilder.NET.Tests;

public class CombinedPolicyBuilder
{
    private readonly IConfiguration _configuration;

    public CombinedPolicyBuilder(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IAsyncPolicy<HttpResponseMessage> Build()
    {
        var retryPolicy = CreateRetryPolicy();
        var circuitBreakerPolicy = CreateCircuitBreakerPolicy();
        var timeoutPolicy = CreateTimeoutPolicy();
        var chaosPolicy = CreateChaosPolicy();
        var latencyPolicy = CreateLatencyPolicy();

        return Policy.WrapAsync(retryPolicy, circuitBreakerPolicy, timeoutPolicy, /*chaosPolicy,*/ latencyPolicy);
    }

    private IAsyncPolicy<HttpResponseMessage> CreateRetryPolicy()
    {
        var retryCount = int.Parse(_configuration["Resilience:RetryCount"]);
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    private IAsyncPolicy<HttpResponseMessage> CreateCircuitBreakerPolicy()
    {
        var failureThreshold = int.Parse(_configuration["Resilience:CircuitBreakerFailureThreshold"]);
        var durationOfBreak = TimeSpan.FromSeconds(int.Parse(_configuration["Resilience:CircuitBreakerDurationOfBreak"]));
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(failureThreshold, durationOfBreak);
    }

    private IAsyncPolicy<HttpResponseMessage> CreateTimeoutPolicy()
    {
        var timeoutSeconds = int.Parse(_configuration["Resilience:TimeoutSeconds"]);
        return Policy.TimeoutAsync<HttpResponseMessage>(timeoutSeconds);
    }
    private IAsyncPolicy<HttpResponseMessage> CreateChaosPolicy()
    {
        var injectFault = bool.Parse(_configuration["Chaos:InjectFault"]);
        var faultInjectionRate = double.Parse(_configuration["Chaos:FaultInjectionRate"]);
        var result = new HttpResponseMessage(HttpStatusCode.BadRequest);

        var chaosPolicy = MonkeyPolicy.InjectResultAsync<HttpResponseMessage>(with =>
            with.Result(result)
                .InjectionRate(faultInjectionRate)
                .Enabled(injectFault)
        );

        return chaosPolicy;
    }


    private IAsyncPolicy<HttpResponseMessage> CreateLatencyPolicy()
    {
        var latencyInjectionRate = double.Parse(_configuration["Chaos:LatencyInjectionRate"]);
        var injectedLatencyMilliseconds = int.Parse(_configuration["Chaos:InjectedLatencyMilliseconds"]);

        return MonkeyPolicy.InjectLatencyAsync<HttpResponseMessage>(with =>
            with.Latency(TimeSpan.FromMilliseconds(injectedLatencyMilliseconds))
                .InjectionRate(latencyInjectionRate)
                .Enabled(true));
    }
}

