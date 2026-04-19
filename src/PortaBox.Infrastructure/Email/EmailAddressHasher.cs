using System.Security.Cryptography;
using System.Text;

namespace PortaBox.Infrastructure.Email;

public static class EmailAddressHasher
{
    public static string Hash(string emailAddress)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(emailAddress);

        var normalized = emailAddress.Trim().ToLowerInvariant();
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
