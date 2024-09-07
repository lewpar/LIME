using LIME.Shared.Network;

namespace LIME.Agent.Network.Packets;

public class StatisticPacket : ILimePacket
{
    public long Min { get; set; }
    public long Max { get; set; }

    public long Current { get; set; }

    public LimeStatistic Statistic { get; set; }

    public StatisticPacket(LimeStatistic statistic, long min, long max, long current)
    {
        Statistic = statistic;

        Min = min;
        Max = max;

        Current = current;
    }

    public byte[] Serialize()
    {
        var ms = new MemoryStream();

        ms.Write(BitConverter.GetBytes((int)LimeOpCodes.CMSG_STATISTIC));
        ms.Write(BitConverter.GetBytes((int)Statistic));
        ms.Write(BitConverter.GetBytes(Min));
        ms.Write(BitConverter.GetBytes(Max));
        ms.Write(BitConverter.GetBytes(Current));

        return ms.ToArray();
    }
}
