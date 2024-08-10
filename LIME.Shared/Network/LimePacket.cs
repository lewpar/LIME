namespace LIME.Shared.Network;

public class LimePacket
{
    public LimePacketType Type { get; set; }
    public byte[]? Data { get; set; }

    public LimePacket(LimePacketType type)
    {
        Type = type;
    }

    public byte[] Build()
    {
        if (Data is null || Data.Length < 1)
        {
            throw new Exception("Tried to convert packet to data but data was null or empty.");
        }

        var ms = new MemoryStream();

        ms.Write(BitConverter.GetBytes((int)Type));
        ms.Write(BitConverter.GetBytes(Data.Length));
        ms.Write(Data);

        return ms.ToArray();
    }
}
