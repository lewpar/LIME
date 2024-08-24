using LIME.Shared.Database;

using System.Text.Json;

namespace LIME.Dashboard.Configuration;

public class LimeDashboardConfig
{
    public const string PATH = "./dashboard.json";

    public MySqlSettings MySql { get; set; }

    public LimeDashboardConfig()
    {
        MySql = new MySqlSettings()
        {
            Host = "localhost",
            Database = "lime",
            User = "root",
            Pass = "root"
        };
    }

    public async Task SaveAsync()
    {
        using var fs = File.OpenWrite(PATH);
        await JsonSerializer.SerializeAsync(fs, this);
    }

    public static async Task<LimeDashboardConfig?> LoadAsync()
    {
        using var fs = File.OpenRead(PATH);
        return await JsonSerializer.DeserializeAsync<LimeDashboardConfig>(fs);
    }
}
