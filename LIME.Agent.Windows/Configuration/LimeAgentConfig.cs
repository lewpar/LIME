using System.Text.Json;

namespace LIME.Agent.Windows.Configuration;

public class LimeAgentConfig
{
    public const string PATH = "./agent.json";

    public string MediatorAddress { get; set; }
    public int MediatorPort { get; set; }

    public LimeAgentConfig()
    {
        MediatorAddress = "127.0.0.1";
        MediatorPort = 55123;
    }

    public async Task SaveAsync()
    {
        using var fs = File.OpenWrite(PATH);
        await JsonSerializer.SerializeAsync(fs, this);
    }

    public static async Task<LimeAgentConfig?> LoadAsync()
    {
        using var fs = File.OpenRead(PATH);
        return await JsonSerializer.DeserializeAsync<LimeAgentConfig>(fs);
    }
}
