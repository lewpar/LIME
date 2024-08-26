using LIME.Shared.Crypto;
using System.Security.Cryptography;
using System.Text;

namespace LIME.Shared.Extensions;

public static class StringExtensions
{
    public static string ToBase64(this string value)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
    }

    public static string FromBase64(this string value)
    {
        return Encoding.UTF8.GetString(Convert.FromBase64String(value));
    }

    public static RSACryptoServiceProvider ToRSACryptoProvider(this string base64)
    {
        var xml = base64.FromBase64();
        var rsa = new RSACryptoServiceProvider(RSAKeypair.KEY_SIZE);
        rsa.FromXmlString(xml);

        return rsa;
    }
}
