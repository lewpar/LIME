namespace LIME.Mediator.Network.Events;

internal class ClientAuthenticationFailedEventArgs : EventArgs
{
    public string Message { get; set; }

    public ClientAuthenticationFailedEventArgs(string message)
    {
        Message = message;
    }
}
