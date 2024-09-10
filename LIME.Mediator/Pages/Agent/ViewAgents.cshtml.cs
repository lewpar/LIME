using LIME.Mediator.Database;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using Microsoft.EntityFrameworkCore;

namespace LIME.Mediator.Pages.Agent;

public class ViewAgentsModel : PageModel
{
    public List<Database.Models.Agent> Agents { get; set; }

    private readonly LimeDbContext dbContext;

    public ViewAgentsModel(LimeDbContext dbContext)
    {
        this.dbContext = dbContext;
        Agents = new List<Database.Models.Agent>();
    }

    public async Task<IActionResult> OnGetAsync()
    {
        Agents = await dbContext.Agents.ToListAsync();

        return Page();
    }
}
