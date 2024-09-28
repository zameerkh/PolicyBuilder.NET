using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Contrib.Simmy;
using Polly.Extensions.Http;
using Refit;
using Polly.Contrib.Simmy.Latency;
using Polly.Contrib.Simmy.Outcomes;
using Xunit;
using ResilientRefit.Core.Proxy;
using FluentAssertions;
using System.Net;

public class IntegrationTests
{
    private readonly IHttpBinClient _httpBinClient;
    private readonly IConfiguration _configuration;

    public IntegrationTests()
    {
        // Load the appsettings.integrationtests.json
        _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.integrationtests.json")
            .Build();

        // Set up the service collection for dependency injection
        var serviceCollection = new ServiceCollection();

        // Read configuration values
        var baseUrl = _configuration["BaseUrl"];
        var retryCount = int.Parse(_configuration["Resilience:RetryCount"]);
        var circuitBreakerFailureThreshold = int.Parse(_configuration["Resilience:CircuitBreakerFailureThreshold"]);
        var circuitBreakerDuration = TimeSpan.FromSeconds(int.Parse(_configuration["Resilience:CircuitBreakerDurationOfBreak"]));
        var timeoutSeconds = int.Parse(_configuration["Resilience:TimeoutSeconds"]);
        var injectFault = bool.Parse(_configuration["Chaos:InjectFault"]);
        var faultInjectionRate = double.Parse(_configuration["Chaos:FaultInjectionRate"]);
        var latencyInjectionRate = double.Parse(_configuration["Chaos:LatencyInjectionRate"]);
        var injectedLatencyMilliseconds = int.Parse(_configuration["Chaos:InjectedLatencyMilliseconds"]);

        // Setup Polly and Simmy policies
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

        var circuitBreakerPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(circuitBreakerFailureThreshold, circuitBreakerDuration);

        var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(timeoutSeconds);


        // Following example causes the policy to return a bad request HttpResponseMessage with a probability of 5% if enabled
        var result = new HttpResponseMessage(HttpStatusCode.BadRequest);
        var chaosPolicy = MonkeyPolicy.InjectResultAsync<HttpResponseMessage>(with =>
            with.Result(result)
                .InjectionRate(faultInjectionRate)
                .Enabled()
        );



        var latencyPolicy = MonkeyPolicy.InjectLatencyAsync<HttpResponseMessage>(with =>
            with.Latency(TimeSpan.FromMilliseconds(injectedLatencyMilliseconds))  // Inject 2 seconds of delay
                .InjectionRate(latencyInjectionRate)  // Inject latency 40% of the time
                .Enabled(true));     // Enable latency injection

        // Configure Refit client with Polly and Simmy policies
        serviceCollection.AddRefitClient<IHttpBinClient>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(baseUrl))
            .AddPolicyHandler(retryPolicy)
            .AddPolicyHandler(circuitBreakerPolicy)
            .AddPolicyHandler(timeoutPolicy)
            .AddPolicyHandler(chaosPolicy)
            .AddPolicyHandler(latencyPolicy);

        // Build service provider
        var serviceProvider = serviceCollection.BuildServiceProvider();
        _httpBinClient = serviceProvider.GetRequiredService<IHttpBinClient>();
    }


    [Fact]
    public async Task Test_Resilience_With_Chaos()
    {
        // Loop through each scenario defined in appsettings.integrationtests.json
        var testScenarios = _configuration.GetSection("TestScenarios").Get<TestScenario[]>();

        foreach (var scenario in testScenarios)
        {
            Console.WriteLine($"Testing scenario with InjectFault: {scenario.InjectFault}, RetryCount: {scenario.RetryCount}, Timeout: {scenario.TimeoutSeconds}s");

            // Here you can reconfigure policies dynamically per test scenario if needed
            // In a more advanced setup, you could dynamically adjust the policies or create new DI scopes

            // Perform API call
            try
            {
                var result = await _httpBinClient.GetAsync();
                result.Should().NotBeNull("because a valid result is expected from the API call");
                Console.WriteLine($"API Call Success: {result}");
            }
            catch (Exception ex)
            {
                // Test fails if exception is thrown
                Console.WriteLine($"API Call Failed: {ex.Message}");
                ex.Should().BeNull("because no exception is expected from the API call");
            }
        }
    }
}

public class TestScenario
{
    public bool InjectFault { get; set; }
    public int RetryCount { get; set; }
    public int TimeoutSeconds { get; set; }
}
