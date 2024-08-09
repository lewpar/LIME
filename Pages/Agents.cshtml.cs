using LIME.Database;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LIME.Pages
{
    public class AgentsModel : PageModel
    {
        private readonly LimeDbContext dbContext;

        public List<Database.Tables.Agents> Agents { get; set; } = new List<Database.Tables.Agents>();

        public AgentsModel(LimeDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            Agents.AddRange(dbContext.Agents.ToList());

            return Page();
        }
    }
}
