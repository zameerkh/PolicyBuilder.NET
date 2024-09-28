namespace ResilientRefit.Core.Models;

public class ResiliencySettings
{
    public RetryPolicySettings RetryPolicy { get; set; }
    public CircuitBreakerPolicySettings CircuitBreakerPolicy { get; set; }
    public TimeoutPolicySettings TimeoutPolicy { get; set; }
}