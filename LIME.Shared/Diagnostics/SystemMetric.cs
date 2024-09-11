namespace LIME.Shared.Diagnostics;

public class SystemMetric
{
    public long Current { get; set; }

    public long Min { get; set; }
    public long Max { get; set; }

    public SystemMetric(long current, long min, long max)
    {
        Current = current;

        Min = min;
        Max = max;
    }
}
