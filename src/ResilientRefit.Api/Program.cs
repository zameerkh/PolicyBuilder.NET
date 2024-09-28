using System.Net;
using Microsoft.OpenApi.Models;
using Polly;
using Refit;
using ResilientRefit.Core.Proxy;

namespace ResilientRefit.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Load configuration from appsettings.json
        builder.Configuration
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables();

        ConfigureServices(builder.Services, builder.Configuration);

        var app = builder.Build();

        ConfigurePipeline(app);

        app.Run();
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Add services to the container.
        services.AddControllers();

        // Add Swagger services to the DI container
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "My API",
                Description = "A simple example ASP.NET Core Web API",
            });

            // Include XML comments if available
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });

        // Add PolicyRegistry for Polly
        var policyRegistry = services.AddPolicyRegistry();

// Define Polly Retry Policy using the nested structure
        IAsyncPolicy < HttpResponseMessage > retryPolicy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.InternalServerError)
            .WaitAndRetryAsync(
                retryCount: configuration.GetValue<int>("HttpBin:ResiliencyPolicies:RetryPolicy:Count"),
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(configuration.GetValue<int>("HttpBin:ResiliencyPolicies:RetryPolicy:Delay"))
            );

        // Define Polly Circuit Breaker Policy using the nested structure
        IAsyncPolicy<HttpResponseMessage> circuitBreakerPolicy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.InternalServerError)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: configuration.GetValue<int>("HttpBin:ResiliencyPolicies:CircuitBreakerPolicy:Count"),
                durationOfBreak: TimeSpan.FromSeconds(configuration.GetValue<int>("HttpBin:ResiliencyPolicies:CircuitBreakerPolicy:Duration"))
            );

        // Define Polly Timeout Policy using the nested structure
        IAsyncPolicy<HttpResponseMessage> timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(
            TimeSpan.FromSeconds(configuration.GetValue<int>("HttpBin:ResiliencyPolicies:TimeoutPolicy:Duration"))
        );

        // Combine all Polly policies into a single wrapped policy
        IAsyncPolicy<HttpResponseMessage> combinedPolicy = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy, timeoutPolicy);

        // Register the combined policy in the PolicyRegistry
        policyRegistry.Add("CombinedPolicy", combinedPolicy);

        // Read the base URL from appsettings.json
        var httpBinBaseUrl = configuration["HttpBin:BaseUrl"];

        // Register the Refit client
        services.AddRefitClient<IHttpBinClient>() // This line requires the Refit.HttpClientFactory namespace
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(httpBinBaseUrl))
            .AddPolicyHandlerFromRegistry("CombinedPolicy"); // Apply the single combined policy
    }

    private static void ConfigurePipeline(WebApplication app)
    {
        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Resilient Refit V1");
                c.RoutePrefix = "swagger"; // Explicitly set RoutePrefix to "swagger"
            });
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();
    }
}
