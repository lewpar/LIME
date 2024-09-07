namespace LIME.Shared.Diagnostics;

public class PerformanceMetric
{
    public long Current { get; set; }

    public long Min { get; set; }
    public long Max { get; set; }

    public PerformanceMetric(long current, long min, long max)
    {
        Current = current;

        Min = min;
        Max = max;
    }
}
