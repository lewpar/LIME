using System.Diagnostics;
using System.Text.RegularExpressions;

namespace LIME.Shared.Diagnostics;

public class WindowsSystemInfoProvider : ISystemInfoProvider
{
    public async Task<string> GetMemoryInfoAsync()
    {
        var startInfo = new ProcessStartInfo()
        {
            FileName = "wmic",
            Arguments = "OS get FreePhysicalMemory,TotalVisibleMemorySize/Value",
            RedirectStandardOutput = true
        };

        using var process = Process.Start(startInfo);

        if (process is null)
        {
            throw new Exception("Process failed to start.");
        }

        return await process.StandardOutput.ReadToEndAsync();
    }

    public async Task<SystemMetric> GetMemoryMetricAsync()
    {
        var info = await GetMemoryInfoAsync();
        var matches = Regex.Matches(info, "(.+)=(.*)");

        long? free = null;
        long? max = null;

        foreach (Match match in matches)
        {
            var key = match.Groups[1].Value;
            var value = match.Groups[2].Value;

            switch (key)
            {
                case "TotalVisibleMemorySize":
                    max = long.Parse(value);
                    break;

                case "FreePhysicalMemory":
                    free = long.Parse(value);
                    break;
            }
        }

        if (max is null || free is null)
        {
            throw new Exception("Failed to parse memory.");
        }

        // wmic measured in kb, so we convert to bytes.
        return new SystemMetric((max.Value - free.Value) * 1000, 0, max.Value * 1000);
    }
}
