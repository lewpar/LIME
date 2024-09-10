using LIME.Shared.Models;

using System.Net.Security;

namespace LIME.Agent.Services;

public class JobContext
{
    public JobType Type { get; set; }

    public required SslStream Stream { get; set; }
}
