namespace PortaBox.Application.Abstractions.Storage;

public sealed class ObjectStorageReference
{
    public ObjectStorageReference(string key, string contentType, long sizeBytes, string sha256)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentException.ThrowIfNullOrWhiteSpace(sha256);

        if (sizeBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeBytes), "Object size cannot be negative.");
        }

        if (sha256.Length != 64)
        {
            throw new ArgumentException("SHA-256 hash must be a 64-character hexadecimal string.", nameof(sha256));
        }

        Key = key;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        Sha256 = sha256;
    }

    public string Key { get; }

    public string ContentType { get; }

    public long SizeBytes { get; }

    public string Sha256 { get; }
}
