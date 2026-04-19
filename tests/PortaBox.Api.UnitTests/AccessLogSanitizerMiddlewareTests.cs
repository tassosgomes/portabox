using PortaBox.Api.Middleware;

namespace PortaBox.Api.UnitTests;

public sealed class AccessLogSanitizerMiddlewareTests
{
    [Theory]
    [InlineData("?token=abc123XYZ", "?token=[REDACTED]")]
    [InlineData("?token=abc&next=1", "?token=[REDACTED]&next=1")]
    [InlineData("?foo=bar&token=secret&baz=qux", "?foo=bar&token=[REDACTED]&baz=qux")]
    [InlineData("?TOKEN=MixedCase", "?TOKEN=[REDACTED]")]
    [InlineData("?token=", "?token=")]
    [InlineData("", "")]
    [InlineData("?foo=bar", "?foo=bar")]
    public void SanitizeQueryString_RedactsTokenValue(string input, string expected)
    {
        var result = AccessLogSanitizerMiddleware.SanitizeQueryString(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void SanitizeQueryString_NullInput_ReturnsNull()
    {
        var result = AccessLogSanitizerMiddleware.SanitizeQueryString(null!);
        Assert.Null(result);
    }

    [Fact]
    public void SanitizeQueryString_DoesNotModifyPathSegments()
    {
        const string query = "?redirect=/setup-password?token=realtoken&other=1";
        var result = AccessLogSanitizerMiddleware.SanitizeQueryString(query);
        Assert.Contains("[REDACTED]", result);
        Assert.DoesNotContain("realtoken", result);
    }
}
