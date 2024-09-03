namespace LIME.CLI.Commands;

internal class CreateCertificateCmd : LimeCommand
{
    public override string Command => "gen-cert";
    public override string Description => "Creates a RSA 2048 certiciate signed by an intermediate certificate.";
    public override string Usage => "gen-cert subject=lime.agent int=./intermediate.pfx role=client";

    public override CommandResult TryExecute()
    {
        //var subject = GetArg("subject");
        //var intPath = GetArg("int");
        //var role = GetArg("role");
        //var intPassword = GetArg("intpassword");

        //if(!TryGetAuthRole(role, out X509CertificateAuthRole authRole))
        //{
        //    return new CommandResult(false, $"Invalid auth role '{role}'.");
        //}

        //try
        //{
        //    var intCertificate = LimeCertificate.ImportCertificate(intPath, intPassword);
        //    if (intCertificate is null)
        //    {
        //        return new CommandResult(false, $"Intermediate certificate not found at '{intPath}'.");
        //    }

        //    var certificate = LimeCertificate.CreateSignedCertificate(intCertificate, subject, authRole, "");

        //    File.WriteAllBytes($"{subject}.pfx", certificate);

        //    return new CommandResult(true, $"Created certificate with subject '{subject}'.");
        //}
        //catch (Exception ex)
        //{
        //    return new CommandResult(false, $"{ex.Message}");
        //}

        return new CommandResult(true);
    }
}
