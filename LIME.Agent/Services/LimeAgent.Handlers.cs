using LIME.Shared.Extensions;

using Microsoft.Extensions.Logging;
using System.Net.Security;
using System.Text;

namespace LIME.Agent.Services;

public partial class LimeAgent
{
    private async Task HandleDisconnectAsync(SslStream stream)
    {
        var dataLength = await stream.ReadIntAsync();
        var data = await stream.ReadBytesAsync(dataLength);

        logger.LogInformation($"Disconnected: {Encoding.UTF8.GetString(data)}");

        connected = false;
    }
}
