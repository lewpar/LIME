using System.Security.Cryptography;

namespace LIME.Shared.Crypto;

public class RSAKeypair
{
    public const int KEY_SIZE = 2048;

    public required string PublicKey { get; set; }
    public required string PrivateKey { get; set; }

    public static RSAKeypair Generate()
    {
        using var rsa = new RSACryptoServiceProvider(KEY_SIZE);

        var privateKey = rsa.ToXmlString(true);
        var publicKey = rsa.ToXmlString(false);

        return new RSAKeypair()
        {
            PublicKey = publicKey,
            PrivateKey = privateKey
        };
    }
}
