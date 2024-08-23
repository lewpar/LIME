using System.Security.Cryptography;

namespace LIME.Shared.Crypto;

public class RSAKeypair
{
    public required string PublicKey { get; set; }
    public required string PrivateKey { get; set; }

    public static RSAKeypair Generate()
    {
        using var rsa = RSA.Create(2048);

        var privateKey = rsa.ExportRSAPrivateKeyPem();
        var publicKey = rsa.ExportRSAPublicKeyPem();

        return new RSAKeypair()
        {
            PublicKey = publicKey,
            PrivateKey = privateKey
        };
    }
}
