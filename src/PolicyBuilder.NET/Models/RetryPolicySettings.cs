namespace PolicyBuilder.NET.Models;

public class RetryPolicySettings
{
    public int Count { get; set; }
    public int Delay { get; set; }
    public int Jitter { get; set; }
}