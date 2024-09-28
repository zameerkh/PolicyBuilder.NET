using ResilientRefit.Api.Extensions;

namespace ResilientRefit.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        ConfigureConfiguration(builder.Configuration);
        builder.Services.ConfigureServices(builder.Configuration);

        var app = builder.Build();

        app.ConfigurePipeline();

        app.Run();
    }

    private static void ConfigureConfiguration(ConfigurationManager configuration)
    {
        configuration
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables();
    }
}