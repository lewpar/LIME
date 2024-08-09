using System.ComponentModel.DataAnnotations;
using System.Net;

namespace LIME.Attributes;

public class ValidateIPAddressAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if(value is not string address)
        {
            return false;
        }

        return IPAddress.TryParse(address, out _);
    }
}
