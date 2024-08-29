using LIME.Shared.Configuration;

using System.Text.Json;

namespace LIME.Agent.Windows.Configuration;

public class LimeAgentConfig
{
    public const string PATH = "./agent.json";

    public string MediatorAddress { get; set; }
    public int MediatorPort { get; set; }

    public CertificateIdentifier Certificate { get; set; }

    public LimeAgentConfig()
    {
        MediatorAddress = "127.0.0.1";
        MediatorPort = 55123;

        Certificate = new CertificateIdentifier("Lime.Mediator", "Lime.Agent");
    }

    public async Task SaveAsync()
    {
        using var fs = File.OpenWrite(PATH);
        await JsonSerializer.SerializeAsync(fs, this);
    }

    public static async Task<LimeAgentConfig?> LoadAsync()
    {
        if(!File.Exists(PATH))
        {
            return null;
        }

        using var fs = File.OpenRead(PATH);
        return await JsonSerializer.DeserializeAsync<LimeAgentConfig>(fs, new JsonSerializerOptions()
        {
            WriteIndented = true
        });
    }
}
