namespace PortaBox.Application.Abstractions.Storage;

public interface IObjectStorage
{
    Task<ObjectStorageReference> UploadAsync(
        string key,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<Uri> GetDownloadUrlAsync(
        string key,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}
