using LIME.Shared.Database;

using System.Text.Json;

namespace LIME.Mediator.Configuration;

public class LimeMediatorConfig
{
    public const string PATH = "./mediator.json";

    public string MediatorBindAddress { get; set; }
    public int MediatorListenPort { get; set; }

    public string CertificateThumbprint { get; set; }

    public MySqlSettings MySql { get; set; }

    public LimeMediatorConfig()
    {
        MediatorBindAddress = "0.0.0.0";
        MediatorListenPort = 55123;

        CertificateThumbprint = string.Empty;

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

    public static async Task<LimeMediatorConfig?> LoadAsync()
    {
        using var fs = File.OpenRead(PATH);
        return await JsonSerializer.DeserializeAsync<LimeMediatorConfig>(fs);
    }
}
