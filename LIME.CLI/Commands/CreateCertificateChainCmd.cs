namespace LIME.CLI.Commands;

internal class CreateCertificateChainCmd : LimeCommand
{
    public override string Command => "gen-chain";
    public override string Description => "Creates a certificate chainwith root and intermediate certificate and end-entity certificate.";
    public override string Usage => "gen-chain root=./root.pfx rootpassword=hunter1 int=./int.pfx intpassword=hunter2 cert=agent.pfx";

    public override CommandResult TryExecute()
    {
        //var rootPath = GetArg("root");
        //var rootPassword = GetArg("rootpassword");
        //var intPath = GetArg("int");
        //var intPassword = GetArg("intpassword");
        //var certPath = GetArg("cert");

        //try
        //{
        //    var rootCertificate = LimeCertificate.ImportCertificate(rootPath, rootPassword);
        //    if (rootCertificate is null)
        //    {
        //        return new CommandResult(false, $"Root certificate not found at '{rootPath}'.");
        //    }

        //    var intCertificate = LimeCertificate.ImportCertificate(intPath, intPassword);
        //    if (intCertificate is null)
        //    {
        //        return new CommandResult(false, $"Intermediate certificate not found at '{intPath}'.");
        //    }

        //    var certificate = LimeCertificate.ImportCertificate(certPath);
        //    if (certificate is null)
        //    {
        //        return new CommandResult(false, $"End-entity certificate not found at '{certPath}'.");
        //    }

        //    var chain = LimeCertificate.CreateBundledCertificate(rootCertificate, intCertificate, certificate);
        //    if(chain is null)
        //    {
        //        return new CommandResult(false, "Failed to create certificate chain.");
        //    }

        //    File.WriteAllBytes($"chain.pfx", chain);

        //    CertificateRevocationListBuilder test = CertificateRevocationListBuilder.Load(new byte[] { 0x01 }, out BigInteger test1);

        //    return new CommandResult(true, $"Created certificate chain.");
        //}
        //catch (Exception ex)
        //{
        //    return new CommandResult(false, $"{ex.Message}");
        //}

        return new CommandResult(true);
    }
}
