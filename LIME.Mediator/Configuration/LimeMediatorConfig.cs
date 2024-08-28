﻿using LIME.Shared.Configuration;
using LIME.Shared.Database;

using System.Text.Json;

namespace LIME.Mediator.Configuration;

public class LimeMediatorConfig
{
    public const string PATH = "./mediator.json";

    public string MediatorBindAddress { get; set; }
    public int MediatorListenPort { get; set; }

    public CertificateIdentifier Certificate { get; set; }

    public LimeMediatorConfig()
    {
        MediatorBindAddress = "0.0.0.0";
        MediatorListenPort = 55123;

        Certificate = new CertificateIdentifier("LIME", "");
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
        using var fs = File.OpenRead(PATH);
        return await JsonSerializer.DeserializeAsync<LimeMediatorConfig>(fs);
    }
}
