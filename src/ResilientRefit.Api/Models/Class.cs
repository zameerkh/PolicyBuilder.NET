namespace ResilientRefit.Api.Models
{
    namespace ResilientRefit.Api
    {
        public class ResiliencySettings
        {
            public RetryPolicySettings RetryPolicy { get; set; }
            public CircuitBreakerPolicySettings CircuitBreakerPolicy { get; set; }
            public TimeoutPolicySettings TimeoutPolicy { get; set; }
        }

        public class RetryPolicySettings
        {
            public int Count { get; set; }
            public int Delay { get; set; }
            public int Jitter { get; set; }
        }

        public class CircuitBreakerPolicySettings
        {
            public int Count { get; set; }
            public int Duration { get; set; }
        }

        public class TimeoutPolicySettings
        {
            public int Duration { get; set; }
        }
    }

}
