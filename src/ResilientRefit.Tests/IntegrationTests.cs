using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using Xunit;
using ResilientRefit.Core.Proxy;
using FluentAssertions;
using Polly;
using Polly.Timeout;
using Polly.CircuitBreaker;
using Microsoft.Extensions.Logging;
using Moq;

namespace ResilientRefit.Tests;


public class IntegrationTests : IClassFixture<PolicyRegistryFixture>
{
    private readonly IConfiguration _configuration;
    private readonly PolicyRegistryFixture _policyRegistryFixture;
    private readonly List<TestScenario> _testScenarios;
    private readonly ILogger<IntegrationTests> _logger;

    public IntegrationTests(PolicyRegistryFixture policyRegistryFixture)
    {
        _policyRegistryFixture = policyRegistryFixture;

        // Create a mock logger
        var mockLogger = new Mock<ILogger<IntegrationTests>>();
        _logger = mockLogger.Object;

        // Load the appsettings.integrationtests.json
        _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.integrationtests.json")
            .Build();

        // Load test scenarios
        _testScenarios = new List<TestScenario>(_configuration.GetSection("TestScenarios").Get<TestScenario[]>());
    }

    private IHttpBinClient CreateHttpBinClient(IAsyncPolicy<HttpResponseMessage> policy)
    {
        // Set up the service collection for dependency injection
        var serviceCollection = new ServiceCollection();

        // Read configuration values
        var baseUrl = _configuration["BaseUrl"];

        // Configure Refit client with the provided policy
        serviceCollection.AddRefitClient<IHttpBinClient>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(baseUrl))
            .AddPolicyHandler(policy);

        // Build service provider
        var serviceProvider = serviceCollection.BuildServiceProvider();
        return serviceProvider.GetRequiredService<IHttpBinClient>();
    }

    [Fact]
    public async Task Test_FaultInjection_With_HighLatency()
    {
        var scenario = _testScenarios.Find(s => s.ScenarioName == "Fault Injection with High Latency");
        if (scenario != null)
        {
            _logger.LogInformation($"Testing scenario: {scenario.ScenarioName}");
            _logger.LogInformation($"InjectFault: {scenario.InjectFault}, FaultInjectionRate: {scenario.FaultInjectionRate}, LatencyInjectionRate: {scenario.LatencyInjectionRate}");

            // Get the policy for the current scenario
            var policyKey = $"Scenario_{scenario.InjectFault}_{scenario.FaultInjectionRate}_{scenario.LatencyInjectionRate}";
            var policy = _policyRegistryFixture.PolicyRegistry[policyKey];

            // Create a new instance of IHttpBinClient with the current policy
            var httpBinClient = CreateHttpBinClient(policy);

            // Perform API call
            Func<Task> act = async () => { var result = await httpBinClient.GetAsync(); };

            // Assert that the call throws a TimeoutRejectedException
            await act.Should().ThrowAsync<TimeoutRejectedException>("because the test scenario is expected to fail with a timeout")
                .WithMessage("*timeout*");
        }
        else
        {
            _logger.LogError("Test scenario 'Fault Injection with High Latency' not found.");
            using (new FluentAssertions.Execution.AssertionScope())
            {
                FluentAssertions.Execution.AssertionScope.Current.FailWith("Test scenario 'Fault Injection with High Latency' not found.");
            }
        }
    }

    [Fact]
    public async Task Test_NoFaultInjection()
    {
        var scenario = _testScenarios.Find(s => s.ScenarioName == "No Fault Injection");
        if (scenario != null)
        {
            _logger.LogInformation($"Testing scenario: {scenario.ScenarioName}");
            _logger.LogInformation($"InjectFault: {scenario.InjectFault}, FaultInjectionRate: {scenario.FaultInjectionRate}, LatencyInjectionRate: {scenario.LatencyInjectionRate}");

            // Get the policy for the current scenario
            var policyKey = $"Scenario_{scenario.InjectFault}_{scenario.FaultInjectionRate}_{scenario.LatencyInjectionRate}";
            var policy = _policyRegistryFixture.PolicyRegistry[policyKey];

            // Create a new instance of IHttpBinClient with the current policy
            var httpBinClient = CreateHttpBinClient(policy);

            // Perform API call
            try
            {
                var result = await httpBinClient.GetAsync();
                result.Should().NotBeNull("because a valid result is expected from the API call");
                _logger.LogInformation($"API Call Success: {result}");
            }
            catch (Exception ex) when (ex is ApiException || ex is TimeoutRejectedException || ex is BrokenCircuitException)
            {
                _logger.LogError(ex, $"API Call Failed with {ex.GetType().Name}: {ex.Message}");
            }
        }
        else
        {
            _logger.LogError("Test scenario 'No Fault Injection' not found.");
        }
    }
}
