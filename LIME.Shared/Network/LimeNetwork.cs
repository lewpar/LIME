using System.Net.Sockets;

namespace LIME.Shared.Network;

public static class LimeNetwork
{
    public static async Task<byte[]> ReadBytesAsync(this NetworkStream stream, int length)
    {
        var buffer = new byte[length];

        await stream.ReadAtLeastAsync(buffer, length);

        return buffer;
    }

    public static async Task<int> ReadIntAsync(this NetworkStream stream)
    {
        return BitConverter.ToInt32(await ReadBytesAsync(stream, sizeof(int)));
    }

    public static async Task<LimePacketType> ReadPacketTypeAsync(this NetworkStream stream)
    {
        return (LimePacketType)await ReadIntAsync(stream);
    }

    public static async Task WriteBytesAsync(this NetworkStream stream, byte[] bytes, int length)
    {
        await stream.WriteAsync(bytes, 0, length);
    }
}
