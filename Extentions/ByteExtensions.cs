using System.Text;

namespace LIME.Extentions;

public static class ByteExtensions
{
    public static string ToHexString(this byte[] bytes)
    {
        var sb = new StringBuilder();

        foreach(var b in bytes)
        {
            sb.Append(b.ToString("X2"));
        }

        return sb.ToString();
    }
}
