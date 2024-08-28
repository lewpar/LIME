using System.ComponentModel.DataAnnotations;
using System.Net;

namespace LIME.Mediator.Validators;

/// <summary>
/// Specified if an input string is a valid <see cref="System.Net.IPAddress">IP Address</see>
/// </summary>
public class IPAddressValidatorAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if(value is not string ipAddress)
        {
            return false;
        }

        return IPAddress.TryParse(ipAddress, out _);
    }
}
