using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using PortaBox.Api.IntegrationTests.Fixtures;

namespace PortaBox.Api.IntegrationTests.Features.OptInDocuments;

/// <summary>
/// Tests document upload: PDF lands in MinIO, metadata is persisted, and download URL is returned.
/// </summary>
[Collection(nameof(AppFactoryCollection))]
public sealed class UploadTests(AppFactoryFixture factory)
{
    [Fact]
    public async Task UploadDocument_1MbPdf_ShouldStoreInMinioAndReturnPresignedUrl()
    {
        await factory.ResetAsync();

        using var client = await factory.CreateOperatorClientAsync();

        // Arrange: create a condominio first
        var createResponse = await client.PostAsJsonAsync("/api/v1/admin/condominios", new
        {
            nomeFantasia = "Residencial Upload",
            cnpj = "45.723.174/0001-10",
            enderecoLogradouro = (string?)null,
            enderecoNumero = (string?)null,
            enderecoComplemento = (string?)null,
            enderecoBairro = (string?)null,
            enderecoCidade = (string?)null,
            enderecoUf = (string?)null,
            enderecoCep = (string?)null,
            administradoraNome = (string?)null,
            optIn = new
            {
                dataAssembleia = "2026-04-01",
                quorumDescricao = "Maioria simples",
                signatarioNome = "Carlos da Silva",
                signatarioCpf = "529.982.247-25",
                dataTermo = "2026-04-02"
            },
            sindico = new
            {
                nome = "Ana Oliveira",
                email = $"upload-{Guid.NewGuid():N}@portabox.test",
                celularE164 = "+5511999880001"
            }
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var condominioId = createBody.GetProperty("condominioId").GetGuid();

        // Act: upload a ~1MB PDF
        const int fileSizeBytes = 1024 * 1024; // 1 MB
        var pdfContent = new byte[fileSizeBytes];
        new Random(42).NextBytes(pdfContent);

        using var multipart = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(pdfContent);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
        multipart.Add(fileContent, "file", "ata-assembleia.pdf");

        var uploadResponse = await client.PostAsync(
            $"/api/v1/admin/condominios/{condominioId}/opt-in-documents?kind=Ata",
            multipart);

        Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);

        var uploadBody = await uploadResponse.Content.ReadFromJsonAsync<JsonElement>();
        var documentId = uploadBody.GetProperty("documentId").GetGuid();
        Assert.NotEqual(Guid.Empty, documentId);

        // Assert: download URL is accessible
        var downloadResponse = await client.GetAsync(
            $"/api/v1/admin/condominios/{condominioId}/opt-in-documents/{documentId}:download");

        Assert.Equal(HttpStatusCode.OK, downloadResponse.StatusCode);

        var downloadBody = await downloadResponse.Content.ReadFromJsonAsync<JsonElement>();
        var url = downloadBody.GetProperty("url").GetString();
        Assert.False(string.IsNullOrWhiteSpace(url));

        // Assert: presigned URL is accessible (download from MinIO)
        using var httpClient = new HttpClient();
        var fileResponse = await httpClient.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, fileResponse.StatusCode);

        var downloadedBytes = await fileResponse.Content.ReadAsByteArrayAsync();
        Assert.Equal(fileSizeBytes, downloadedBytes.Length);
    }
}
