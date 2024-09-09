using LIME.Mediator.Database;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using Microsoft.EntityFrameworkCore;

namespace LIME.Mediator.Pages.Agent;

public class ViewAgentModel : PageModel
{
    public string? Error { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Guid { get; set; }

    public Database.Models.Agent? Agent { get; set; }

    private readonly LimeDbContext dbContext;

    public ViewAgentModel(LimeDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if(string.IsNullOrWhiteSpace(Guid))
        {
            Error = "No guid.";
            return Page();
        }

        var agent = await dbContext.Agents.FirstOrDefaultAsync(a => a.Guid.ToString() == Guid);
        if(agent is null)
        {
            Error = "No agent found with that guid.";
            return Page();
        }

        Agent = agent;

        return Page();
    }
}
