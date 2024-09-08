using LIME.Mediator.Network.Packets;

using LIME.Shared.Models;
using LIME.Shared.Network;

using System.Net;
using System.Net.Security;
using System.Net.Sockets;

namespace LIME.Mediator.Network;

/// <summary>
/// Represents a client that connects to a LIME server.
/// </summary>
public class LimeClient
{
    /// <summary>
    /// Gets or sets the unique identifier for the Lime client.
    /// </summary>
    public required Guid Guid { get; set; }

    /// <summary>
    /// Gets or sets the current state of the Lime client.
    /// </summary>
    public required LimeClientState State { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the last heartbeat received from the client.
    /// </summary>
    public DateTimeOffset LastHeartbeat { get; set; }

    /// <summary>
    /// Gets or sets the TCP socket used for communication with the server.
    /// </summary>
    public TcpClient Socket { get; set; }

    /// <summary>
    /// Gets or sets the SSL stream used for secure communication with the server.
    /// </summary>
    public SslStream Stream { get; set; }

    /// <summary>
    /// Gets or sets the endpoint representing the IP address and port of the client.
    /// </summary>
    public LimeEndpoint Endpoint { get; set; }

    /// <summary>
    /// Gets the queue of tasks scheduled to be sent to the client.
    /// </summary>
    public Queue<LimeTask> Tasks { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LimeClient"/> class with the specified TCP client and SSL stream.
    /// </summary>
    /// <param name="client">The TCP client representing the connection.</param>
    /// <param name="stream">The SSL stream used for secure communication.</param>
    public LimeClient(TcpClient client, SslStream stream)
    {
        Socket = client;
        Stream = stream;

        Endpoint = new LimeEndpoint(IPAddress.Any.MapToIPv4().ToString(), 0);

        Tasks = new Queue<LimeTask>();
    }

    /// <summary>
    /// Asynchronously sends a packet to the server through the SSL stream.
    /// </summary>
    /// <param name="packet">The packet to send, implementing <see cref="ILimePacket"/>.</param>
    /// <returns>A task that represents the asynchronous send operation.</returns>
    public async Task SendPacketAsync(ILimePacket packet)
    {
        if (Stream is null || !Stream.CanWrite)
        {
            return;
        }

        await Stream.WriteAsync(packet.Serialize());
    }

    /// <summary>
    /// Asynchronously disconnects the client from the server by sending a <see cref="DisconnectPacket">disconnect packet</see> and closing the socket.
    /// </summary>
    /// <param name="message">An optional message to include in the disconnect packet.</param>
    /// <returns>A task that represents the asynchronous disconnect operation.</returns>
    public async Task DisconnectAsync(string message = "")
    {
        try
        {
            var packet = new DisconnectPacket(message);
            await Stream.WriteAsync(packet.Serialize());
        }
        catch {}
        finally
        {
            Socket.Close();
            State = LimeClientState.Disconnected;
        }
    }

    /// <summary>
    /// Queues a <see cref="LimeTask">task</see> to be sent to the client.
    /// </summary>
    /// <param name="task">The task to be queued.</param>
    public void QueueTask(LimeTask task)
    {
        Tasks.Enqueue(task);
    }
}
