using LIME.Mediator.Configuration;
using LIME.Mediator.Database;
using LIME.Mediator.Database.Models;
using LIME.Mediator.Pages.Models;

using LIME.Shared.Crypto;

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
    public string Certificate { get; set; } = string.Empty;

    [BindProperty]
    public CreateAgentDto Model { get; set; } = default!;

    public CreateAgentModel(LimeDbContext dbContext, LimeMediatorConfig config)
    {
        this.dbContext = dbContext;
        this.config = config;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!this.ModelState.IsValid)
        {
            return Page();
        }

        var rootCert = LimeCertificate.GetCertificate(config.Mediator.RootCertificate.Thumbprint, StoreName.Root);
        if (rootCert is null)
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

        var agentCert = LimeCertificate.CreateClientCertificate(intCert, config.Agent.Certificate.Subject);
        var crlBuilder = new CertificateRevocationListBuilder();

        if (crlBuilder is null)
        {
            ErrorMessage = "Failed to create Certificate Revocation List builder.";
            return Page();
        }

        var chain = new X509Certificate2Collection()
        {
            rootCert,
            new X509Certificate2(intCert.Export(X509ContentType.Cert)),
            new X509Certificate2(agentCert.Export(X509ContentType.Pkcs12, ""), "", X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable),
        };

        if (chain is null)
        {
            ErrorMessage = "Failed to form certificate chain, agent not created.";
            return Page();
        }

        var pfx = chain.Export(X509ContentType.Pkcs12);
        if (pfx is null)
        {
            ErrorMessage = "Failed to export certificate chain.";
            return Page();
        }

        var existingAgent = await dbContext.Agents.FirstOrDefaultAsync(a => a.Address == Model.IPAddress);
        if (existingAgent is not null)
        {
            ErrorMessage = "An agent with that IP Address already exists.";
            return Page();
        }

        await dbContext.Agents.AddAsync(new Database.Models.Agent()
        {
            Status = AgentStatus.Unknown,
            Guid = Guid.NewGuid(),
            Address = Model.IPAddress,
            Name = Model.Name,
            Thumbprint = agentCert.Thumbprint
        });

        var rows = await dbContext.SaveChangesAsync();
        if (rows < 1)
        {
            ErrorMessage = "An internal error occured, the agent was not created.";
            return Page();
        }

        StatusMessage = "Agent created successfully. Install the certificate below on the agent to enable connection to the mediator.";

        Certificate = LimeCertificate.ConvertCertificateChainToPem(chain, agentCert);

        return Page();
    }
}
