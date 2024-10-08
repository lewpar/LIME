﻿using LIME.Shared.Configuration;

using System.Text.Json;

namespace LIME.Agent.Configuration;

public class LimeAgentConfig
{
    public const string PATH = "./agent.json";

    public string MediatorHost { get; set; }
    public string MediatorAddress { get; set; }
    public int MediatorPort { get; set; }

    public int HeartbeatFrequency { get; set; }
    public int TaskFrequency { get; set; }

    public CertificateIdentifier Certificate { get; set; }

    public LimeAgentConfig()
    {
        MediatorHost = "LIME Mediator";
        MediatorAddress = "127.0.0.1";
        MediatorPort = 55123;

        HeartbeatFrequency = 15;
        TaskFrequency = 3;

        Certificate = new CertificateIdentifier("Lime.Intermediate", "Lime.Agent");
    }

    public async Task SaveAsync()
    {
        using var fs = File.OpenWrite(PATH);
        await JsonSerializer.SerializeAsync(fs, this, new JsonSerializerOptions()
        {
            WriteIndented = true
        });
    }

    public static async Task<LimeAgentConfig?> LoadAsync()
    {
        if(!File.Exists(PATH))
        {
            return null;
        }

        using var fs = File.OpenRead(PATH);
        return await JsonSerializer.DeserializeAsync<LimeAgentConfig>(fs);
    }
}
