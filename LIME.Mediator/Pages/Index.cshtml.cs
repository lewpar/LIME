using LIME.Mediator.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LIME.Mediator.Pages;

public class IndexModel : PageModel
{
    public LimeMediator Mediator { get; }

    public IndexModel(LimeMediator mediator)
    {
        Mediator = mediator;
    }
}
