using LIME.Shared.Network;
using System.Net.Sockets;

namespace LIME.Mediator.Models;

public class LimeClient
{
    public Socket Socket { get; private set; }
    public NetworkStream Stream { get; private set; }

    public LimeClient(Socket socket, NetworkStream stream)
    {
        this.Socket = socket;
        this.Stream = stream;
    }

    public async Task SendPacketAsync(LimePacket packet)
    {
        if(Stream is null || !Stream.CanWrite)
        {
            return;
        }

        var data = packet.Build();
        await Stream.WriteAsync(data);
    }

    public async Task<LimePacket?> ReadPacketAsync(LimePacketType packetType)
    {
        if (Stream is null || !Stream.CanRead)
        {
            return null;
        }

        try
        {
            var buffer = new byte[sizeof(int)];

            await Stream.ReadAsync(buffer, 0, sizeof(int));
            var limePacketType = (LimePacketType)BitConverter.ToInt32(buffer);

            await Stream.ReadAsync(buffer, 0, sizeof(int));
            var dataLength = BitConverter.ToInt32(buffer);

            buffer = new byte[dataLength];

            await Stream.ReadAsync(buffer, 0, dataLength);

            var packet = new LimePacket(limePacketType);
            packet.Data = buffer;

            return packet;
        }
        catch(Exception)
        {
            return null;
        }
    }
}
