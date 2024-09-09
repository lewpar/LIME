using LIME.Shared.Models;

using System.Net.Security;

namespace LIME.Agent.Services;

public class TaskContext
{
    public required LimeTask Task { get; set; }
    public required SslStream Stream { get; set; }
}
