﻿using System.Net.Sockets;

namespace LIME.Mediator.Network.Events;

public class ClientAuthenticationFailedEventArgs : EventArgs
{
    public TcpClient Client { get; }
    public string Message { get; }

    public ClientAuthenticationFailedEventArgs(TcpClient client, string message)
    {
        Client = client;
        Message = message;
    }
}
