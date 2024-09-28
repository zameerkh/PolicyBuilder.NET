using Polly.Extensions.Http;
using Polly;
using System.Net;

namespace ResilientRefit.Api
{
    public class PolicyBuilder
    {
        private readonly List<IAsyncPolicy<HttpResponseMessage>> _policies = new();

        public PolicyBuilder WithCircuitBreakerPolicy(int failureThreshold, TimeSpan durationOfBreak)
        {
            var circuitBreakerPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(failureThreshold, durationOfBreak,
                    onBreak: (response, timespan) =>
                    {
                        Console.WriteLine($"Circuit broken for {timespan.TotalSeconds} seconds due to: {response.Exception?.Message}");
                    },
                    onReset: () =>
                    {
                        Console.WriteLine("Circuit reset.");
                    },
                    onHalfOpen: () =>
                    {
                        Console.WriteLine("Circuit half-open, testing the connection.");
                    });

            _policies.Add(circuitBreakerPolicy);
            return this;
        }

        public PolicyBuilder WithRetryPolicy(int retryCount, int delay, int jitter)
        {
            var retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == HttpStatusCode.InternalServerError)
                .WaitAndRetryAsync(retryCount, retryAttempt =>
                {
                    var jitterer = new Random();
                    return TimeSpan.FromSeconds(Math.Pow(delay, retryAttempt)) + TimeSpan.FromMilliseconds(jitterer.Next(0, jitter));
                });

            _policies.Add(retryPolicy);
            return this;
        }

        public PolicyBuilder WithTimeoutPolicy(TimeSpan timeout)
        {
            var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(timeout);
            _policies.Add(timeoutPolicy);
            return this;
        }

        public IAsyncPolicy<HttpResponseMessage> Build()
        {
            return Policy.WrapAsync(_policies.ToArray());
        }
    }
}
