namespace LIME.Shared.Network;

public class DataUnitConverter
{
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
