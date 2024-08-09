using LIME.Attributes;

using System.ComponentModel.DataAnnotations;

namespace LIME.Models.Agents;

public class AddAgentDataModel
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "You must enter a name.")]
    [MinLength(1, ErrorMessage = "Agent names must be between 1 - 128 characters.")]
    [MaxLength(128, ErrorMessage = "Agent names must be between 1 - 128 characters.")]
    public string? Name { get; set; }

    [Required(AllowEmptyStrings = false, ErrorMessage = "You must enter an IP Address.")]
    [ValidateIPAddress(ErrorMessage = "Invalid IP Address.")]
    public string? IPAddress { get; set; }

    [Required(AllowEmptyStrings = false, ErrorMessage = "You must enter a port.")]
    [ValidatePort(ErrorMessage = "Invalid port.")]
    public string? Port { get; set; }
}
