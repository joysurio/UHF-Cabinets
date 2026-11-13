using System.Security.Cryptography;
using System.Text;

namespace UHFCabinets.Api.Security;

public static class ApiKeyHasher
{
    public static string Hash(string plain)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plain));
        return Convert.ToHexString(bytes);
    }

    public static bool Verify(string plain, string storedHex)
    {
        var computed = Hash(plain);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computed),
            Encoding.UTF8.GetBytes(storedHex.ToUpperInvariant()));
    }
}
