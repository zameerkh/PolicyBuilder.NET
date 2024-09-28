using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;
using Xunit;

namespace PolicyBuilder.NET.Tests;

public class ResiliencyPolicyBuilderTests
{
    private readonly Mock<ILogger<global::PolicyBuilder.NET.ResiliencyPolicyBuilder>> _loggerMock = new();

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new global::PolicyBuilder.NET.ResiliencyPolicyBuilder(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithMessage("*logger*");
    }

    [Fact]
    public void WithCircuitBreakerPolicy_InvalidParameters_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var builder = new global::PolicyBuilder.NET.ResiliencyPolicyBuilder(_loggerMock.Object);

        // Act & Assert
        Action act1 = () => builder.WithCircuitBreakerPolicy(0, TimeSpan.FromSeconds(1));
        act1.Should().Throw<ArgumentOutOfRangeException>().WithMessage("*failureThreshold*");

        Action act2 = () => builder.WithCircuitBreakerPolicy(1, TimeSpan.Zero);
        act2.Should().Throw<ArgumentOutOfRangeException>().WithMessage("*durationOfBreak*");
    }

    [Fact]
    public void WithRetryPolicy_InvalidParameters_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var builder = new global::PolicyBuilder.NET.ResiliencyPolicyBuilder(_loggerMock.Object);

        // Act & Assert
        Action act1 = () => builder.WithRetryPolicy(0, 1, 1);
        act1.Should().Throw<ArgumentOutOfRangeException>().WithMessage("*retryCount*");

        Action act2 = () => builder.WithRetryPolicy(1, 0, 1);
        act2.Should().Throw<ArgumentOutOfRangeException>().WithMessage("*delay*");

        Action act3 = () => builder.WithRetryPolicy(1, 1, -1);
        act3.Should().Throw<ArgumentOutOfRangeException>().WithMessage("*jitter*");
    }

    [Fact]
    public void WithTimeoutPolicy_InvalidParameters_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var builder = new global::PolicyBuilder.NET.ResiliencyPolicyBuilder(_loggerMock.Object);

        // Act & Assert
        Action act = () => builder.WithTimeoutPolicy(TimeSpan.Zero);
        act.Should().Throw<ArgumentOutOfRangeException>().WithMessage("*timeout*");
    }

    [Fact]
    public void Build_NoPoliciesAdded_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = new global::PolicyBuilder.NET.ResiliencyPolicyBuilder(_loggerMock.Object);

        // Act & Assert
        Action act = () => builder.Build();
        act.Should().Throw<InvalidOperationException>().WithMessage("No policies have been added to the builder.");
    }

    [Fact]
    public async Task Build_WithCircuitBreakerPolicy_ExecutesSuccessfully()
    {
        // Arrange
        var builder = new global::PolicyBuilder.NET.ResiliencyPolicyBuilder(_loggerMock.Object)
            .WithCircuitBreakerPolicy(1, TimeSpan.FromSeconds(1));

        var policy = builder.Build();

        // Act
        var result = await policy.ExecuteAsync(() => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Build_WithRetryPolicy_ExecutesSuccessfully()
    {
        // Arrange
        var builder = new global::PolicyBuilder.NET.ResiliencyPolicyBuilder(_loggerMock.Object)
            .WithRetryPolicy(1, 1, 1);

        var policy = builder.Build();

        // Act
        var result = await policy.ExecuteAsync(() => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Build_WithTimeoutPolicy_ExecutesSuccessfully()
    {
        // Arrange
        var builder = new global::PolicyBuilder.NET.ResiliencyPolicyBuilder(_loggerMock.Object)
            .WithTimeoutPolicy(TimeSpan.FromSeconds(1));

        var policy = builder.Build();

        // Act
        var result = await policy.ExecuteAsync(() => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Build_WithAllPolicies_ExecutesSuccessfully()
    {
        // Arrange
        var builder = new global::PolicyBuilder.NET.ResiliencyPolicyBuilder(_loggerMock.Object)
            .WithCircuitBreakerPolicy(1, TimeSpan.FromSeconds(1))
            .WithRetryPolicy(1, 1, 1)
            .WithTimeoutPolicy(TimeSpan.FromSeconds(1));

        var policy = builder.Build();

        // Act
        var result = await policy.ExecuteAsync(() => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CircuitBreakerPolicy_OpensCircuitAfterFailure()
    {
        // Arrange
        var builder = new global::PolicyBuilder.NET.ResiliencyPolicyBuilder(_loggerMock.Object)
            .WithCircuitBreakerPolicy(1, TimeSpan.FromSeconds(1));

        var policy = builder.Build();

        // Act & Assert
        // First call fails
        Func<Task> act1 = () => policy.ExecuteAsync(() => Task.FromException<HttpResponseMessage>(new HttpRequestException()));
        await act1.Should().ThrowAsync<HttpRequestException>();

        // Second call should fail immediately due to open circuit
        Func<Task> act2 = () => policy.ExecuteAsync(() => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        await act2.Should().ThrowAsync<BrokenCircuitException>();
    }

    [Fact]
    public async Task RetryPolicy_RetriesOnFailure()
    {
        // Arrange
        var builder = new global::PolicyBuilder.NET.ResiliencyPolicyBuilder(_loggerMock.Object)
            .WithRetryPolicy(1, 1, 1);

        var policy = builder.Build();

        int attempt = 0;

        // Act
        await policy.ExecuteAsync(() =>
        {
            attempt++;
            if (attempt < 2)
            {
                return Task.FromException<HttpResponseMessage>(new HttpRequestException());
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        // Assert
        attempt.Should().Be(2);
    }

    [Fact]
    public async Task TimeoutPolicy_ThrowsTimeoutRejectedException()
    {
        // Arrange
        var builder = new global::PolicyBuilder.NET.ResiliencyPolicyBuilder(_loggerMock.Object)
            .WithTimeoutPolicy(TimeSpan.FromSeconds(1));

        var policy = builder.Build();

        // Act
        Func<Task<HttpResponseMessage>> act = () => policy.ExecuteAsync(async (context, token) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(2), token); // Pass the cancellation token
            return new HttpResponseMessage(HttpStatusCode.OK);
        }, new Context(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<TimeoutRejectedException>();
    }
}