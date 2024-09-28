namespace PolicyBuilder.NET.Models;

public class CircuitBreakerPolicySettings
{
    public int Count { get; set; }
    public int Duration { get; set; }
}