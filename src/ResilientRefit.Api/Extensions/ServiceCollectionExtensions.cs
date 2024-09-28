using Microsoft.OpenApi.Models;
using Refit;
using ResilientRefit.Api.Models.ResilientRefit.Api;
using ResilientRefit.Core.Proxy;

namespace ResilientRefit.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static void ConfigureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.ConfigureSwagger();
        services.ConfigurePollyPolicies(configuration);
        services.ConfigureRefitClient(configuration);
    }

    public static void ConfigureSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "My API",
                Description = "A simple example ASP.NET Core Web API",
            });

            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });
    }

    public static void ConfigurePollyPolicies(this IServiceCollection services, IConfiguration configuration)
    {
        var policyRegistry = services.AddPolicyRegistry();
        var resiliencySettings = configuration.GetSection("HttpBin:ResiliencyPolicies").Get<ResiliencySettings>();

        var policy = new PolicyBuilder()
            .WithCircuitBreakerPolicy(resiliencySettings.CircuitBreakerPolicy.Count, TimeSpan.FromSeconds(resiliencySettings.CircuitBreakerPolicy.Duration))
            .WithRetryPolicy(resiliencySettings.RetryPolicy.Count, resiliencySettings.RetryPolicy.Delay, resiliencySettings.RetryPolicy.Jitter)
            .WithTimeoutPolicy(TimeSpan.FromSeconds(resiliencySettings.TimeoutPolicy.Duration))
            .Build();

        policyRegistry.Add("CombinedPolicy", policy);
    }

    public static void ConfigureRefitClient(this IServiceCollection services, IConfiguration configuration)
    {
        var httpBinBaseUrl = configuration["HttpBin:BaseUrl"];
        services.AddRefitClient<IHttpBinClient>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(httpBinBaseUrl))
            .AddPolicyHandlerFromRegistry("CombinedPolicy");
    }
}
