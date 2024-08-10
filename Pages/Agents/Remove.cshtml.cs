using LIME.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LIME.Pages.Agents
{
    public class RemoveModel : PageModel
    {
        private readonly LimeDbContext dbContext;

        public int AgentId { get; set; }

        public RemoveModel(LimeDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if(id is null)
            {
                return RedirectToPage("/Agents");
            }

            AgentId = id.Value;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var agentPending = await dbContext.AgentsPending.FirstOrDefaultAsync(a => a.Id == id);

            if(agentPending is not null)
            {
                dbContext.AgentsPending.Remove(agentPending);
            }

            var agent = await dbContext.Agents.FirstOrDefaultAsync(a => a.Id == id);

            if(agent is not null)
            {
                dbContext.Agents.Remove(agent);
            }

            await dbContext.SaveChangesAsync();

            return RedirectToPage("/Agents");
        }
    }
}
