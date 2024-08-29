using LIME.Shared.Configuration;

using System.Text.Json;

namespace LIME.Mediator.Configuration;

public class LimeMediatorConfig
{
    public const string PATH = "./mediator.json";

    public string MediatorBindAddress { get; set; }
    public int MediatorListenPort { get; set; }

    public CertificateIdentifier RootCertificate { get; set; }
    public CertificateIdentifier IntermediateCertificate { get; set; }
    public CertificateIdentifier ServerCertificate { get; set; }
    public CertificateIdentifier AgentCertificate { get; set; }

    public LimeMediatorConfig()
    {
        MediatorBindAddress = "0.0.0.0";
        MediatorListenPort = 55123;

        RootCertificate = new CertificateIdentifier("Lime", "Lime");
        IntermediateCertificate = new CertificateIdentifier("Lime", "Lime.Intermediate");
        ServerCertificate = new CertificateIdentifier("Lime.Intermediate", "Lime.Mediator");
        AgentCertificate = new CertificateIdentifier("Lime.Intermediate", "Lime.Agent");
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
