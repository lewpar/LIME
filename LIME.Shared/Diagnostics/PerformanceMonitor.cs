using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace LIME.Shared.Diagnostics;

public class PerformanceMonitor
{
    public static async Task<PerformanceMetric> MeasureMemoryAsync()
    {
        if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var startInfo = new ProcessStartInfo()
            {
                FileName = "wmic",
                Arguments = "OS get FreePhysicalMemory,TotalVisibleMemorySize/Value",
                RedirectStandardOutput = true
            };

            using var process = Process.Start(startInfo);
            
            if(process is null)
            {
                throw new Exception("Process failed to start.");
            }

            var result = await process.StandardOutput.ReadToEndAsync();
            var matches = Regex.Matches(result, "(.+)=(.*)");

            long? free = null;
            long? max = null;

            foreach(Match match in matches)
            {
                var key = match.Groups[1].Value;
                var value = match.Groups[2].Value;

                switch(key)
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
            return new PerformanceMetric((max.Value - free.Value) * 1000, 0, max.Value * 1000);
        }
        else
        {
            throw new NotImplementedException();
        }
    }
}
