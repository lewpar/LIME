using LIME.Services;

using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LIME.Pages;

public class IndexModel : PageModel
{
    public LimeMediator Mediator { get; set; }

    public IndexModel(LimeMediator mediator)
    {
        this.Mediator = mediator;
    }
}
