using System.Collections.Concurrent;
using System.Security.Cryptography;
using PortaBox.Application.Abstractions.Storage;

namespace PortaBox.Modules.Gestao.UnitTests.TestDoubles;

public sealed class FakeObjectStorage : IObjectStorage
{
    private readonly ConcurrentDictionary<string, StoredObject> _objects = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, StoredObject> Objects => _objects;

    public async Task<ObjectStorageReference> UploadAsync(
        string key,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentNullException.ThrowIfNull(content);

        await using var memoryStream = new MemoryStream();
        await content.CopyToAsync(memoryStream, cancellationToken);

        var payload = memoryStream.ToArray();
        var hash = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();
        _objects[key] = new StoredObject(payload, contentType);

        return new ObjectStorageReference(key, contentType, payload.LongLength, hash);
    }

    public Task<Uri> GetDownloadUrlAsync(
        string key,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_objects.ContainsKey(key))
        {
            throw new KeyNotFoundException($"Object '{key}' was not found.");
        }

        var effectiveTtl = ttl ?? TimeSpan.FromMinutes(5);
        return Task.FromResult(new Uri($"https://fake-storage.local/{Uri.EscapeDataString(key)}?ttl={(int)effectiveTtl.TotalSeconds}"));
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _objects.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public sealed record StoredObject(byte[] Content, string ContentType);
}
