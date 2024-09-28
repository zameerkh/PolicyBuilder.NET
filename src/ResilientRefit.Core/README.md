# ResilientRefit.Core

## Overview

`ResilientRefit.Core` is a library designed to enhance the resilience of Refit clients by integrating Polly policies. This library allows you to configure Refit clients with retry, circuit breaker, and timeout policies, ensuring that your HTTP requests are more robust and fault-tolerant.

## Features

- **Retry Policy**: Automatically retries failed requests a specified number of times with a delay between each attempt.
- **Circuit Breaker Policy**: Monitors the number of consecutive failures and opens the circuit if the threshold is reached, preventing further requests for a specified duration.
- **Timeout Policy**: Enforces a maximum duration for each request, ensuring that long-running requests are terminated.

## Installation

To install `ResilientRefit.Core`, add the following NuGet package to your project:


## Configuration

### appsettings.json

Configure your resiliency policies and Refit client settings in your `appsettings.json` file:

```json
  "HttpBin": {
    "BaseUrl": "https://httpbin.org",
    "ResiliencyPolicies": {
            "RetryPolicy": {
        "Count": 3,
        "Delay": 2,
        "Jitter": 1000
      },
      "CircuitBreakerPolicy": {
        "Count": 5,
        "Duration": 5
      },
      "TimeoutPolicy": {
        "Duration": 10
      }
    }
  },
```

### Example Usage

#### 1. Define Your Refit Interface

Create an interface for your Refit client:
**Renders as:**
```csharp
public interface IMyRefitClient { [Get("/endpoint")] Task<ApiResponse> GetEndpointAsync(); }
```


## API Reference

### `PollyPoliciesExtensions`

#### `ConfigureRefitClient<TClient>`

Configures a Refit client with Polly policies.

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

     +    services.ConfigureRefitClient<IMyRefitClient>(configuration, "MyRefitClient");
    }
}

```


- **TClient**: The type of the Refit client.
- **services**: The service collection.
- **configuration**: The configuration.
- **sectionName**: The configuration section name.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Contributing

Contributions are welcome! Please open an issue or submit a pull request for any changes.

## Contact

If you have any questions, suggestions, or would like to contribute to this project, feel free to reach out!

- **Email**: [zameer.kh@gmail.com](mailto:zameer.kh@gmail.com)
- **GitHub**: [@zameerkh](https://github.com/zameerkh)

You can also open an issue on [GitHub Issues](https://github.com/zameerkh/ResilientRefit/issues) if you encounter any problems or have feature requests.








