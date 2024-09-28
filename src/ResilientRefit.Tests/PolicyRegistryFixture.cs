using Microsoft.Extensions.Configuration;
using Polly;
using System.Net.Http;

namespace ResilientRefit.Tests;

public class PolicyRegistryFixture : IDisposable
{
    public IReadOnlyDictionary<string, IAsyncPolicy<HttpResponseMessage>> PolicyRegistry { get; }

    public PolicyRegistryFixture()
    {
        // Load the appsettings.integrationtests.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.integrationtests.json")
            .Build();

        // Read resilience settings
        var resilienceSettings = configuration.GetSection("Resilience");

        // Read test scenarios
        var testScenarios = configuration.GetSection("TestScenarios").Get<TestScenario[]>();

        // Create a dictionary to hold the policies
        var policyRegistry = new Dictionary<string, IAsyncPolicy<HttpResponseMessage>>();

        // Create policies for each test scenario
        foreach (var scenario in testScenarios)
        {
            var scenarioConfiguration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "Resilience:RetryCount", resilienceSettings["RetryCount"] },
                    { "Resilience:CircuitBreakerFailureThreshold", resilienceSettings["CircuitBreakerFailureThreshold"] },
                    { "Resilience:CircuitBreakerDurationOfBreak", resilienceSettings["CircuitBreakerDurationOfBreak"] },
                    { "Resilience:TimeoutSeconds", resilienceSettings["TimeoutSeconds"] },
                    { "Chaos:InjectFault", scenario.InjectFault.ToString() },
                    { "Chaos:FaultInjectionRate", scenario.FaultInjectionRate.ToString() },
                    { "Chaos:LatencyInjectionRate", scenario.LatencyInjectionRate.ToString() },
                    { "Chaos:InjectedLatencyMilliseconds", scenario.InjectedLatencyMilliseconds.ToString() }
                })
                .Build();

            var combinedPolicyBuilder = new CombinedPolicyBuilder(scenarioConfiguration);
            var combinedPolicy = combinedPolicyBuilder.Build();

            policyRegistry.Add($"Scenario_{scenario.InjectFault}_{scenario.FaultInjectionRate}_{scenario.LatencyInjectionRate}", combinedPolicy);
        }

        PolicyRegistry = policyRegistry;
    }

    public void Dispose()
    {
        // Cleanup if necessary
    }
}
public class TestScenario
{
    public string ScenarioName { get; set; }
    public bool InjectFault { get; set; }
    public double FaultInjectionRate { get; set; }
    public double LatencyInjectionRate { get; set; }
    public int InjectedLatencyMilliseconds { get; set; }
    public bool ExpectedOutcome { get; set; }
}


