using LIME.Mediator.Database;
using LIME.Mediator.Database.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LIME.Mediator.Pages;

public class IndexModel : PageModel
{
    public List<Agent> Agents { get; set; }

    private readonly LimeDbContext dbContext;

    public IndexModel(LimeDbContext dbContext)
    {
        this.dbContext = dbContext;
        Agents = new List<Agent>();
    }

    public async Task<IActionResult> OnGetAsync()
    {
        Agents = await dbContext.Agents.ToListAsync();

        return Page();
    }
}
