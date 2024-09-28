namespace ResilientRefit.Tests.Models;

public class TestScenario
{
    public string ScenarioName { get; set; }
    public bool InjectFault { get; set; }
    public double FaultInjectionRate { get; set; }
    public double LatencyInjectionRate { get; set; }
    public int InjectedLatencyMilliseconds { get; set; }
    public bool ExpectedOutcome { get; set; }
}