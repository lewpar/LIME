namespace LIME.Shared.Diagnostics;

public interface ISystemInfoProvider
{
    public Task<string> GetMemoryInfoAsync();
    public Task<SystemMetric> GetMemoryMetricAsync();
}
