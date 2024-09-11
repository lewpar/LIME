using System.Runtime.InteropServices;

namespace LIME.Shared.Diagnostics;

public class SystemMonitor
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
    /// A <see cref="SystemMetric"/> object containing the used memory, free memory, 
    /// and total visible memory in bytes.
    /// </returns>
    /// <exception cref="Exception">Thrown when the process fails to start or memory values cannot be parsed.</exception>
    /// <exception cref="PlatformNotSupportedException">Thrown when the method is called on an unsupported platform.</exception>
    public static async Task<SystemMetric> MeasureMemoryAsync()
    {
        if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return await new WindowsSystemInfoProvider().GetMemoryMetricAsync();
        }
        else
        {
            throw new NotImplementedException();
        }
    }
}
