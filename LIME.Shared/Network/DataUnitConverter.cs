﻿namespace LIME.Shared.Network;

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
        if(bytes < 0)
        {
            throw new DataUnitConverterException("Bytes cannot be negative.");
        }

        DataUnitType[] units = { DataUnitType.B, DataUnitType.KB, DataUnitType.MB, DataUnitType.GB, DataUnitType.TB };

        float amount = (float)bytes;
        int unitIndex = 0;

        while (amount >= 1024f && unitIndex < units.Length - 1)
        {
            amount /= 1024f;
            unitIndex++;
        }

        return new DataUnit(amount, units[unitIndex]);
    }
}
