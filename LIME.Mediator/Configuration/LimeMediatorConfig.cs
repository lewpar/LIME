using System.Text.Json;

namespace LIME.Mediator.Configuration;

public class LimeMediatorConfig
{
    public const string PATH = "./mediator.json";

    public LimeMediatorSettings Mediator { get; set; }
    public LimeDashboardSettings Dashboard { get; set; }
    public LimeAgentSettings Agent { get; set; }

    public LimeMediatorConfig()
    {
        Mediator = new LimeMediatorSettings();
        Dashboard = new LimeDashboardSettings();
        Agent = new LimeAgentSettings();
    }

    public async Task SaveAsync()
    {
        File.Delete(PATH);

        using var fs = File.OpenWrite(PATH);
        await JsonSerializer.SerializeAsync(fs, this, new JsonSerializerOptions()
        {
            WriteIndented = true
        });
    }

    public static async Task<LimeMediatorConfig?> LoadAsync()
    {
        if(!File.Exists(PATH))
        {
            return null;
        }

        using var fs = File.OpenRead(PATH);
        return await JsonSerializer.DeserializeAsync<LimeMediatorConfig>(fs);
    }
}
