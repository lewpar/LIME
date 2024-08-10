using LIME.Database;
using LIME.Database.Tables;
using LIME.Extentions;
using LIME.Models.Agents;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using System.Security.Cryptography;

namespace LIME.Pages.Agents;

public class AddModel : PageModel
{
    public string? Error { get; private set; }
    public string? AddSecret { get; private set; }

    private readonly LimeDbContext dbContext;

    public AddModel(LimeDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IActionResult> OnPostAsync([FromForm]AddAgentDataModel model)
    {
        if(model is null)
        {
            return Page();
        }

        var secret = Guid.NewGuid().ToByteArray().ToHexString();

        var pending = new AgentsPending()
        {
            CreatedDate = DateTime.Now,
            ExpireDate = DateTime.Now.AddHours(12),
            Secret = secret
        };

        var agentPending = await dbContext.AgentsPending.AddAsync(pending);
        var agent = new Database.Tables.Agents()
        {
            Id = pending.Id,
            Name = model.Name,
            Address = model.IPAddress,
            Activated = false
        };

        await dbContext.Agents.AddAsync(agent);

        var rows = await dbContext.SaveChangesAsync();
        if(rows < 2)
        {
            Error = "An internal error occured.";
            return Page();
        }

        AddSecret = secret;

        return Page();
    }
}
