using LIME.Shared.Network;
using System.Net.Security;
using System.Net.Sockets;

namespace LIME.Shared.Extensions;

public static class StreamExtensions
{
    public static async Task<byte[]> ReadBytesAsync(this SslStream stream, int length, CancellationToken cancellationToken = default)
    {
        var buffer = new byte[length];

        await stream.ReadAtLeastAsync(buffer, length, cancellationToken: cancellationToken);

        return buffer;
    }

    public static async Task<int> ReadIntAsync(this SslStream stream, CancellationToken cancellationToken = default)
    {
        return BitConverter.ToInt32(await ReadBytesAsync(stream, sizeof(int), cancellationToken));
    }

    public static async Task<LimePacketType> ReadPacketTypeAsync(this SslStream stream)
    {
        return (LimePacketType)await ReadIntAsync(stream);
    }

    public static async Task WriteBytesAsync(this SslStream stream, byte[] bytes)
    {
        await stream.WriteAsync(bytes);
    }
}
