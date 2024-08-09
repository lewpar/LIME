using System.ComponentModel.DataAnnotations;

namespace LIME.Attributes;

public class ValidatePortAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if(value is not string port)
        {
            return false;
        }

        if(!int.TryParse(port, out int p))
        {
            return false;
        }

        if(p < 1)
        {
            return false;
        }

        return true;
    }
}
