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

    public static async Task<long> ReadLongAsync(this SslStream stream, CancellationToken cancellationToken = default)
    {
        return BitConverter.ToInt64(await ReadBytesAsync(stream, sizeof(long), cancellationToken));
    }

    public static async Task<T?> ReadEnumAsync<T>(this SslStream stream) where T : struct, IConvertible
    {
        var value = await ReadIntAsync(stream);

        if(!typeof(T).IsEnum)
        {
            return null;
        }

        if (!Enum.IsDefined(typeof(T), value))
        {
            return default;
        }

        return (T)(object)value;
    }

    public static async Task WriteBytesAsync(this SslStream stream, byte[] bytes)
    {
        await stream.WriteAsync(bytes);
    }
}
