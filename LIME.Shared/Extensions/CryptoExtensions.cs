using System.Security.Cryptography.X509Certificates;

namespace LIME.Shared.Extensions;

public static class CryptoExtensions
{
    public static void Store(this X509Certificate2 cert, StoreName storeName, StoreLocation storeLocation = StoreLocation.CurrentUser)
    {
        var store = new X509Store(storeName, storeLocation, OpenFlags.ReadWrite);
        store.Add(cert);
    }
}
