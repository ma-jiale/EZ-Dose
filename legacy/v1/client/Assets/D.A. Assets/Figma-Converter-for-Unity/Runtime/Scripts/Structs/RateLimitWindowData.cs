using System;

namespace DA_Assets.FCU
{
    [Serializable]
    public struct RateLimitWindowData
    {
        public double WaitSeconds { get; set; }
        public string RateLimitDetails { get; set; }
        public DateTime CreatedAtUtc { get; set; }

        public DateTime WaitUntilUtc => CreatedAtUtc.AddSeconds(Math.Max(0d, WaitSeconds));
    }

    public enum RateLimitWindowAction
    {
        None = 0,
        WaitCompleted = 1,
        RetryNow = 2,
        StopImport = 3
    }

    [Serializable]
    public struct RateLimitWindowResult
    {
        public RateLimitWindowAction Action { get; set; }

        public bool ShouldRetry => Action == RateLimitWindowAction.RetryNow || Action == RateLimitWindowAction.WaitCompleted;
        public bool ShouldStop => Action == RateLimitWindowAction.StopImport;
        public bool IsDefault => Action == RateLimitWindowAction.None;
    }
}
