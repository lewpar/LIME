using LIME.Mediator.Configuration;
using LIME.Mediator.Database;
using LIME.Mediator.Pages.Models;

using LIME.Shared.Crypto;
using LIME.Shared.Database.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using Microsoft.EntityFrameworkCore;

using System.Security.Cryptography.X509Certificates;

namespace LIME.Mediator.Pages;

public class CreateAgentModel : PageModel
{
    private readonly LimeDbContext dbContext;
    private readonly LimeMediatorConfig config;

    public string StatusMessage { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;

    [BindProperty]
    public CreateAgentDto Model { get; set; } = default!;

    public CreateAgentModel(LimeDbContext dbContext, LimeMediatorConfig config)
    {
        this.dbContext = dbContext;
        this.config = config;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if(!this.ModelState.IsValid)
        {
            return Page();
        }

        var rootCert = LimeCertificate.GetCertificate(config.Mediator.RootCertificate.Thumbprint, StoreName.Root);
        if(rootCert is null)
        {
            ErrorMessage = "Failed to fetch root certificate, the agent was not created.";
            return Page();
        }

        var intCert = LimeCertificate.GetCertificate(config.Mediator.IntermediateCertificate.Thumbprint, StoreName.CertificateAuthority);
        if (intCert is null)
        {
            ErrorMessage = "Failed to fetch intermediate certificate, the agent was not created.";
            return Page();
        }

        var agentCert = LimeCertificate.CreateSignedCertificate(intCert, config.Agent.Certificate.Subject, X509CertificateAuthRole.Client);

        var existingAgent = await dbContext.Agents.FirstOrDefaultAsync(a => a.Address == Model.IPAddress);
        if(existingAgent is not null)
        {
            ErrorMessage = "An agent with that IP Address already exists.";
            return Page();
        }

        var certificateChain = new X509Certificate2Collection()
        {
            new X509Certificate2(rootCert.Export(X509ContentType.Cert)),
            new X509Certificate2(intCert.Export(X509ContentType.Cert)),
            agentCert
        };

        var certificate = certificateChain.Export(X509ContentType.Pfx);
        if(certificate is null)
        {
            ErrorMessage = "Failed to form certificate chain, agent not created.";
            return Page();
        }

        await dbContext.Agents.AddAsync(new Agent()
        {
            Address = Model.IPAddress,
            Name = Model.Name
        });

        var rows = await dbContext.SaveChangesAsync();
        if(rows < 1)
        {
            ErrorMessage = "An internal error occured, the agent was not created.";
            return Page();
        }

        StatusMessage = "Created agent. Install the downloaded certificate on the agent to allow connection to the mediator.";

        return File(certificate, "application/x-pkcs12", $"{Model.Name}.pfx");
    }
}
