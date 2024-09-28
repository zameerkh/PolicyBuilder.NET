using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Polly;
using Xunit;
using PolicyBuilder = ResilientRefit.Core.PolicyBuilder;

namespace ResilientRefit.Tests;

public class PolicyBuilderIntegrationTests
{
    private readonly Mock<ILogger<PolicyBuilder>> _loggerMock = new();
    private readonly IConfiguration _configuration;

    public PolicyBuilderIntegrationTests()
    {
        var configurationBuilder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.integrationtests.json");
        _configuration = configurationBuilder.Build();
    }

    [Fact]
    public async Task FaultInjectionWithHighLatency()
    {
        var scenario = _configuration.GetSection("TestScenarios")
            .Get<List<TestScenario>>()
            .First(s => s.ScenarioName == "Fault Injection with High Latency");

        // Arrange
        var builder = new PolicyBuilder(_loggerMock.Object)
            .WithRetryPolicy(_configuration.GetValue<int>("Resilience:RetryCount"), 1, 1)
            .WithCircuitBreakerPolicy(_configuration.GetValue<int>("Resilience:CircuitBreakerFailureThreshold"), TimeSpan.FromSeconds(_configuration.GetValue<int>("Resilience:CircuitBreakerDurationOfBreak")))
            .WithTimeoutPolicy(TimeSpan.FromSeconds(_configuration.GetValue<int>("Resilience:TimeoutSeconds")));

        var policy = builder.Build();

        // Act
        Func<Task<HttpResponseMessage>> act = () => policy.ExecuteAsync(async (context, token) =>
        {
            if (scenario.InjectFault && new Random().NextDouble() < scenario.FaultInjectionRate)
            {
                throw new HttpRequestException("Injected fault");
            }

            if (new Random().NextDouble() < scenario.LatencyInjectionRate)
            {
                await Task.Delay(scenario.InjectedLatencyMilliseconds, token);
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        }, new Context(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task NoFaultInjection()
    {
        var scenario = _configuration.GetSection("TestScenarios")
            .Get<List<TestScenario>>()
            .First(s => s.ScenarioName == "No Fault Injection");

        // Arrange
        var builder = new PolicyBuilder(_loggerMock.Object)
            .WithRetryPolicy(_configuration.GetValue<int>("Resilience:RetryCount"), 1, 1)
            .WithCircuitBreakerPolicy(_configuration.GetValue<int>("Resilience:CircuitBreakerFailureThreshold"), TimeSpan.FromSeconds(_configuration.GetValue<int>("Resilience:CircuitBreakerDurationOfBreak")))
            .WithTimeoutPolicy(TimeSpan.FromSeconds(_configuration.GetValue<int>("Resilience:TimeoutSeconds")));

        var policy = builder.Build();

        // Act
        Func<Task<HttpResponseMessage>> act = () => policy.ExecuteAsync(async (context, token) =>
        {
            if (scenario.InjectFault && new Random().NextDouble() < scenario.FaultInjectionRate)
            {
                throw new HttpRequestException("Injected fault");
            }

            if (new Random().NextDouble() < scenario.LatencyInjectionRate)
            {
                await Task.Delay(scenario.InjectedLatencyMilliseconds, token);
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        }, new Context(), CancellationToken.None);

        // Assert
        var result = await act();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task FaultInjectionWithoutLatency()
    {
        var scenario = _configuration.GetSection("TestScenarios")
            .Get<List<TestScenario>>()
            .First(s => s.ScenarioName == "Fault Injection without Latency");

        // Arrange
        var builder = new PolicyBuilder(_loggerMock.Object)
            .WithRetryPolicy(_configuration.GetValue<int>("Resilience:RetryCount"), 1, 1)
            .WithCircuitBreakerPolicy(_configuration.GetValue<int>("Resilience:CircuitBreakerFailureThreshold"), TimeSpan.FromSeconds(_configuration.GetValue<int>("Resilience:CircuitBreakerDurationOfBreak")))
            .WithTimeoutPolicy(TimeSpan.FromSeconds(_configuration.GetValue<int>("Resilience:TimeoutSeconds")));

        var policy = builder.Build();

        // Act
        Func<Task<HttpResponseMessage>> act = () => policy.ExecuteAsync(async (context, token) =>
        {
            if (scenario.InjectFault && new Random().NextDouble() < scenario.FaultInjectionRate)
            {
                throw new HttpRequestException("Injected fault");
            }

            if (new Random().NextDouble() < scenario.LatencyInjectionRate)
            {
                await Task.Delay(scenario.InjectedLatencyMilliseconds, token);
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        }, new Context(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task HighLatencyWithoutFaultInjection()
    {
        var scenario = _configuration.GetSection("TestScenarios")
            .Get<List<TestScenario>>()
            .First(s => s.ScenarioName == "High Latency without Fault Injection");

        // Arrange
        var builder = new PolicyBuilder(_loggerMock.Object)
            .WithRetryPolicy(_configuration.GetValue<int>("Resilience:RetryCount"), 1, 1)
            .WithCircuitBreakerPolicy(_configuration.GetValue<int>("Resilience:CircuitBreakerFailureThreshold"), TimeSpan.FromSeconds(_configuration.GetValue<int>("Resilience:CircuitBreakerDurationOfBreak")))
            .WithTimeoutPolicy(TimeSpan.FromSeconds(_configuration.GetValue<int>("Resilience:TimeoutSeconds")));

        var policy = builder.Build();

        // Act
        Func<Task<HttpResponseMessage>> act = () => policy.ExecuteAsync(async (context, token) =>
        {
            if (scenario.InjectFault && new Random().NextDouble() < scenario.FaultInjectionRate)
            {
                throw new HttpRequestException("Injected fault");
            }

            if (new Random().NextDouble() < scenario.LatencyInjectionRate)
            {
                await Task.Delay(scenario.InjectedLatencyMilliseconds, token);
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        }, new Context(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task PartialFaultInjectionWithLatency()
    {
        var scenario = _configuration.GetSection("TestScenarios")
            .Get<List<TestScenario>>()
            .First(s => s.ScenarioName == "Partial Fault Injection with Latency");

        // Arrange
        var builder = new PolicyBuilder(_loggerMock.Object)
            .WithRetryPolicy(_configuration.GetValue<int>("Resilience:RetryCount"), 1, 1)
            .WithCircuitBreakerPolicy(_configuration.GetValue<int>("Resilience:CircuitBreakerFailureThreshold"), TimeSpan.FromSeconds(_configuration.GetValue<int>("Resilience:CircuitBreakerDurationOfBreak")))
            .WithTimeoutPolicy(TimeSpan.FromSeconds(_configuration.GetValue<int>("Resilience:TimeoutSeconds")));

        var policy = builder.Build();

        // Act
        Func<Task<HttpResponseMessage>> act = () => policy.ExecuteAsync(async (context, token) =>
        {
            // Control randomness for testing
            if (scenario.InjectFault)
            {
                throw new HttpRequestException("Injected fault");
            }

            if (scenario.LatencyInjectionRate > 0)
            {
                await Task.Delay(scenario.InjectedLatencyMilliseconds, token);
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        }, new Context(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .Where(e => e is HttpRequestException || e is Polly.CircuitBreaker.BrokenCircuitException);
    }



    [Fact]
    public async Task PartialFaultInjectionWithoutLatency()
    {
        var scenario = _configuration.GetSection("TestScenarios")
            .Get<List<TestScenario>>()
            .First(s => s.ScenarioName == "Partial Fault Injection without Latency");

        // Arrange
        var builder = new PolicyBuilder(_loggerMock.Object)
            .WithRetryPolicy(_configuration.GetValue<int>("Resilience:RetryCount"), 1, 1)
            .WithCircuitBreakerPolicy(_configuration.GetValue<int>("Resilience:CircuitBreakerFailureThreshold"), TimeSpan.FromSeconds(_configuration.GetValue<int>("Resilience:CircuitBreakerDurationOfBreak")))
            .WithTimeoutPolicy(TimeSpan.FromSeconds(_configuration.GetValue<int>("Resilience:TimeoutSeconds")));

        var policy = builder.Build();

        // Act
        Func<Task<HttpResponseMessage>> act = () => policy.ExecuteAsync(async (context, token) =>
        {
            if (scenario.InjectFault && new Random().NextDouble() < scenario.FaultInjectionRate)
            {
                throw new HttpRequestException("Injected fault");
            }

            if (new Random().NextDouble() < scenario.LatencyInjectionRate)
            {
                await Task.Delay(scenario.InjectedLatencyMilliseconds, token);
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        }, new Context(), CancellationToken.None);

        // Assert
        var result = await act();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }


}