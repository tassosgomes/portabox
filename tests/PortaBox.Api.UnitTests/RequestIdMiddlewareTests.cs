using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using PortaBox.Api.Infrastructure;

namespace PortaBox.Api.UnitTests;

public class RequestIdMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WithIncomingRequestId_PropagatesSameHeader()
    {
        var middleware = new RequestIdMiddleware(
            _ => Task.CompletedTask,
            NullLogger<RequestIdMiddleware>.Instance);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[RequestIdMiddleware.HeaderName] = "external-request-id";

        await middleware.InvokeAsync(httpContext);

        Assert.Equal("external-request-id", httpContext.TraceIdentifier);
        Assert.Equal("external-request-id", httpContext.Response.Headers[RequestIdMiddleware.HeaderName].ToString());
    }

    [Fact]
    public async Task InvokeAsync_WithoutIncomingRequestId_GeneratesGuidHeader()
    {
        var middleware = new RequestIdMiddleware(
            _ => Task.CompletedTask,
            NullLogger<RequestIdMiddleware>.Instance);

        var httpContext = new DefaultHttpContext();

        await middleware.InvokeAsync(httpContext);

        Assert.True(Guid.TryParse(httpContext.TraceIdentifier, out _));
        Assert.Equal(httpContext.TraceIdentifier, httpContext.Response.Headers[RequestIdMiddleware.HeaderName].ToString());
    }
}
