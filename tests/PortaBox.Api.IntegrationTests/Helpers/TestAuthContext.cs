using System.Text;
using System.Text.Json;

namespace PortaBox.Api.IntegrationTests.Helpers;

public sealed record TestAuthContext(Guid UserId, Guid? TenantId, string[] Roles)
{
    public const string HeaderName = "X-Test-Auth";

    public static TestAuthContext SindicoOf(Guid tenantId, Guid? userId = null)
    {
        return new TestAuthContext(userId ?? Guid.NewGuid(), tenantId, ["Sindico"]);
    }

    public static TestAuthContext Operator(Guid? userId = null)
    {
        return new TestAuthContext(userId ?? Guid.NewGuid(), null, ["Operator"]);
    }

    public string ToHeaderValue()
    {
        var payload = JsonSerializer.Serialize(new Payload(UserId, TenantId, Roles));
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));
    }

    internal static bool TryParse(string headerValue, out TestAuthContext? context)
    {
        context = null;

        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(headerValue));
            var payload = JsonSerializer.Deserialize<Payload>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));

            if (payload is null || payload.UserId == Guid.Empty || payload.Roles is null || payload.Roles.Length == 0)
            {
                return false;
            }

            context = new TestAuthContext(payload.UserId, payload.TenantId, payload.Roles);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private sealed record Payload(Guid UserId, Guid? TenantId, string[] Roles);
}
