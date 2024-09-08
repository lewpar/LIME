using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace LIME.Shared.Diagnostics;

public class PerformanceMonitor
{
    /// <summary>
    /// Asynchronously measures the current memory usage of the system by retrieving
    /// the total visible memory and free physical memory.
    /// </summary>
    /// <remarks>
    /// This method retrieves system memory information to calculate used memory by 
    /// subtracting free memory from the total visible memory. The results are returned in bytes.
    /// The implementation supports both Windows and Linux platforms.
    /// </remarks>
    /// <returns>
    /// A <see cref="PerformanceMetric"/> object containing the used memory, free memory, 
    /// and total visible memory in bytes.
    /// </returns>
    /// <exception cref="Exception">Thrown when the process fails to start or memory values cannot be parsed.</exception>
    /// <exception cref="PlatformNotSupportedException">Thrown when the method is called on an unsupported platform.</exception>
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
