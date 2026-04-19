namespace PortaBox.Application.Abstractions.Storage;

public static class ObjectStorageKeyFactory
{
    public static string BuildOptInDocumentKey(Guid tenantId, Guid documentId, string contentType)
    {
        var extension = contentType switch
        {
            "application/pdf" => "pdf",
            "image/jpeg" => "jpg",
            "image/png" => "png",
            _ => throw new ArgumentException("Unsupported content type for opt-in document key.", nameof(contentType))
        };

        return $"condominios/{tenantId:D}/opt-in/{documentId:D}.{extension}";
    }
}
