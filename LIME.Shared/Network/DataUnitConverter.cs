namespace LIME.Shared.Network;

public class DataUnitConverter
{
    /// <summary>
    /// Converts a byte value into a human-readable appropriate data unit
    /// based on its size (e.g., B, KB, MB, GB, TB).
    /// </summary>
    /// <param name="bytes">The number of bytes to convert.</param>
    /// <returns>A <see cref="DataUnit"/> object representing the converted value and its 
    /// corresponding data unit in a formatted string.</returns>
    public static DataUnit GetDataUnitFromBytes(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };

        float amount = (float)bytes;
        int unitIndex = 0;

        while (amount >= 1024f && unitIndex < units.Length - 1)
        {
            amount /= 1024f;
            unitIndex++;
        }

        return new DataUnit($"{amount:0.00}", units[unitIndex]);
    }
}
