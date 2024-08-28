using LIME.Mediator.Database;
using LIME.Mediator.Pages.Models;

using LIME.Shared.Crypto;
using LIME.Shared.Database.Models;
using LIME.Shared.Extensions;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LIME.Mediator.Pages;

public class CreateAgentModel : PageModel
{
    private readonly LimeDbContext dbContext;

    public string StatusMessage { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;

    public string PrivateKey { get; set; } = string.Empty;

    [BindProperty]
    public CreateAgentDto Model { get; set; } = default!;

    public CreateAgentModel(LimeDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if(!this.ModelState.IsValid)
        {
            return Page();
        }

        var existingAgent = await dbContext.Agents.FirstOrDefaultAsync(a => a.Address == Model.IPAddress);
        if(existingAgent is not null)
        {
            ErrorMessage = "An agent with that IP Address already exists.";
            return Page();
        }

        var keyPair = RSAKeypair.Generate();

        await dbContext.Agents.AddAsync(new Agent()
        {
            Address = Model.IPAddress,
            Name = Model.Name,
            Key = keyPair.PublicKey.ToBase64()
        });

        var rows = await dbContext.SaveChangesAsync();
        if(rows < 1)
        {
            ErrorMessage = "An internal error occured, the agent was not created.";
            return Page();
        }

        StatusMessage = "Created agent. Save this private key on the agent to connect it to the mediator.";
        PrivateKey = $"{keyPair.PrivateKey.ToBase64()}";

        return Page();
    }
}
