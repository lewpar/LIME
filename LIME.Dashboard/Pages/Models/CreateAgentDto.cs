using LIME.Dashboard.Validators;

using System.ComponentModel.DataAnnotations;

namespace LIME.Dashboard.Pages.Models;

public class CreateAgentDto
{
    [Required]
    [MinLength(1)]
    [MaxLength(255)]
    public required string Name { get; set; }

    [Required]
    [IPAddressValidator]
    public required string IPAddress { get; set; }
}
