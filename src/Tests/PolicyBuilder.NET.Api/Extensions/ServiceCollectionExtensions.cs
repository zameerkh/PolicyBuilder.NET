using Microsoft.OpenApi.Models;
using PolicyBuilder.Api.Proxy;
using PolicyBuilder.NET;
using Refit;

namespace PolicyBuilder.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static void ConfigureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.ConfigureSwagger();
        services.ConfigureRefitClient<IHttpBinClient>(configuration, "HttpBin");
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

    public static void ConfigureRefitClient<TClient>(this IServiceCollection services, IConfiguration configuration, string sectionName) where TClient : class
    {
        // Validate input parameters
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        if (string.IsNullOrEmpty(sectionName)) throw new ArgumentException("Section name cannot be null or empty", nameof(sectionName));

        // Configure Polly policies
        var configuredPolicyName = services.ConfigurePollyPolicies<TClient>(configuration, sectionName);

        // Retrieve the base URL from the configuration
        var baseUrl = configuration[$"{sectionName}:BaseUrl"];
        if (string.IsNullOrEmpty(baseUrl)) throw new InvalidOperationException($"Base URL for section '{sectionName}' is not configured.");

        // Configure the Refit client with the base URL and Polly policies
        services.AddRefitClient<TClient>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(baseUrl))
            .AddPolicyHandlerFromRegistry(configuredPolicyName);
    }
}
