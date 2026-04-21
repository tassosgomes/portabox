using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using PortaBox.Api.IntegrationTests.Fixtures;
using PortaBox.Api.IntegrationTests.Helpers;

namespace PortaBox.Api.IntegrationTests.Features.Estrutura;

[Collection(nameof(PostgresDatabaseCollection))]
public sealed class ContractConformanceTests(PostgresDatabaseFixture fixture)
{
    private static readonly string ContractFilePath = Path.Combine(
        AppContext.BaseDirectory,
        "..",
        "..",
        "..",
        "..",
        "..",
        ".compozy",
        "tasks",
        "f02-gestao-blocos-unidades",
        "api-contract.yaml");

    [Fact]
    public async Task Swagger_ShouldMatchContract_ForF02PathsMethodsAndStatusCodes()
    {
        await fixture.ResetAsync();
        await using var factory = new EstruturaTestApiFactory(fixture.ConnectionString);
        using var client = factory.CreateClient(authContext: null);

        var contract = await LoadOpenApiDocumentAsync(ContractFilePath);
        var swaggerResponse = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, swaggerResponse.StatusCode);
        var swaggerText = await swaggerResponse.Content.ReadAsStringAsync();
        var swagger = new OpenApiStringReader().Read(swaggerText, out var diagnostic);
        Assert.Empty(diagnostic.Errors);

        var contractPaths = contract.Paths.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        var swaggerPaths = swagger.Paths
            .Where(pair => NormalizeSwaggerPath(pair.Key) is { } normalized && contractPaths.ContainsKey(normalized))
            .ToDictionary(pair => NormalizeSwaggerPath(pair.Key)!, pair => pair.Value, StringComparer.Ordinal);

        Assert.Equal(contractPaths.Keys.OrderBy(key => key), swaggerPaths.Keys.OrderBy(key => key));

        foreach (var (path, contractPath) in contractPaths)
        {
            Assert.True(swaggerPaths.TryGetValue(path, out var swaggerPath), $"Swagger nao contem o path {path}.");

            var contractMethods = contractPath.Operations.Keys.OrderBy(key => key.ToString()).ToArray();
            var swaggerMethods = swaggerPath!.Operations.Keys.OrderBy(key => key.ToString()).ToArray();
            Assert.Equal(contractMethods, swaggerMethods);

            foreach (var (method, contractOperation) in contractPath.Operations)
            {
                var swaggerOperation = swaggerPath.Operations[method];
                var contractStatusCodes = contractOperation.Responses.Keys.OrderBy(key => key).ToArray();
                var swaggerStatusCodes = swaggerOperation.Responses.Keys.OrderBy(key => key).ToArray();
                Assert.Equal(contractStatusCodes, swaggerStatusCodes);
            }
        }
    }

    [Fact]
    public async Task HappyResponse_ShouldValidateAgainstEstruturaSchema()
    {
        await fixture.ResetAsync();
        await using var factory = new EstruturaTestApiFactory(fixture.ConnectionString);
        var seeded = await EstruturaTestData.SeedActiveTenantAsync(factory.Services, tenantName: "Residencial Contrato");
        using var client = factory.CreateClient(TestAuthContext.SindicoOf(seeded.CondominioId, seeded.SindicoUserId));

        var response = await client.GetAsync($"/api/v1/condominios/{seeded.CondominioId}/estrutura");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = await EstruturaTestData.ReadJsonAsync(response);
        var contract = await LoadOpenApiDocumentAsync(ContractFilePath);
        var schema = ResolveSchema(
            contract,
            contract.Paths["/condominios/{condominioId}/estrutura"]
                .Operations[OperationType.Get]
                .Responses["200"]
                .Content["application/json"]
                .Schema);

        AssertJsonMatchesSchema(contract, schema, payload.RootElement, "responseBody");
    }

    [Fact]
    public async Task ConflictProblemDetails_ShouldValidateAgainstContractSchema()
    {
        await fixture.ResetAsync();
        await using var factory = new EstruturaTestApiFactory(fixture.ConnectionString);
        var seeded = await EstruturaTestData.SeedActiveTenantAsync(factory.Services);
        using var client = factory.CreateClient(TestAuthContext.SindicoOf(seeded.CondominioId, seeded.SindicoUserId));

        var response = await client.PostAsJsonAsync(
            $"/api/v1/condominios/{seeded.CondominioId}/blocos",
            new { nome = seeded.BlocoNome });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var payload = await EstruturaTestData.ReadJsonAsync(response);

        var contract = await LoadOpenApiDocumentAsync(ContractFilePath);
        var schema = ResolveSchema(contract, contract.Components.Schemas["ProblemDetails"]);
        AssertJsonMatchesSchema(contract, schema, payload.RootElement, "problemDetails");
        Assert.Equal((int)response.StatusCode, payload.RootElement.GetProperty("status").GetInt32());
    }

    private static async Task<OpenApiDocument> LoadOpenApiDocumentAsync(string path)
    {
        var content = await File.ReadAllTextAsync(path);
        content = content.Replace("openapi: 3.1.0", "openapi: 3.0.3", StringComparison.Ordinal);
        var document = new OpenApiStringReader().Read(content, out var diagnostic);
        Assert.Empty(diagnostic.Errors);
        return document;
    }

    private static string? NormalizeSwaggerPath(string path)
    {
        return path.StartsWith("/api/v1", StringComparison.Ordinal)
            ? path[7..]
            : null;
    }

    private static OpenApiSchema ResolveSchema(OpenApiDocument document, OpenApiSchema schema, HashSet<string>? visitedReferences = null)
    {
        if (schema.Reference?.Id is { } referenceId && document.Components.Schemas.TryGetValue(referenceId, out var referenced))
        {
            visitedReferences ??= new HashSet<string>(StringComparer.Ordinal);
            if (!visitedReferences.Add(referenceId))
            {
                return referenced;
            }

            return ResolveSchema(document, referenced, visitedReferences);
        }

        return schema;
    }

    private static void AssertJsonMatchesSchema(OpenApiDocument document, OpenApiSchema schema, JsonElement element, string path)
    {
        schema = ResolveSchema(document, schema);

        if (element.ValueKind == JsonValueKind.Null)
        {
            Assert.True(schema.Nullable, $"{path} nao aceita null pelo contrato.");
            return;
        }

        if (schema.AllOf.Count > 0)
        {
            foreach (var part in schema.AllOf)
            {
                AssertJsonMatchesSchema(document, part, element, path);
            }

            return;
        }

        if (schema.Type == "object" || (schema.Type is null && schema.Properties.Count > 0))
        {
            Assert.Equal(JsonValueKind.Object, element.ValueKind);
            foreach (var required in schema.Required)
            {
                Assert.True(element.TryGetProperty(required, out _), $"{path}.{required} e obrigatorio pelo contrato.");
            }

            foreach (var property in schema.Properties)
            {
                if (element.TryGetProperty(property.Key, out var child))
                {
                    AssertJsonMatchesSchema(document, property.Value, child, $"{path}.{property.Key}");
                }
            }

            return;
        }

        if (schema.Type == "array")
        {
            Assert.Equal(JsonValueKind.Array, element.ValueKind);
            foreach (var item in element.EnumerateArray())
            {
                AssertJsonMatchesSchema(document, schema.Items, item, $"{path}[]");
            }

            return;
        }

        if (schema.Type == "string")
        {
            Assert.Equal(JsonValueKind.String, element.ValueKind);
            var stringValue = element.GetString() ?? string.Empty;
            if (schema.Format == "uuid")
            {
                Assert.True(Guid.TryParse(stringValue, out _), $"{path} deveria ser uuid.");
            }
            else if (schema.Format == "date-time")
            {
                Assert.True(DateTimeOffset.TryParse(stringValue, out _), $"{path} deveria ser date-time.");
            }

            return;
        }

        if (schema.Type == "integer")
        {
            Assert.True(element.ValueKind is JsonValueKind.Number, $"{path} deveria ser numero inteiro.");
            Assert.True(element.TryGetInt32(out _), $"{path} deveria caber em Int32.");
            return;
        }

        if (schema.Type == "boolean")
        {
            Assert.True(element.ValueKind is JsonValueKind.True or JsonValueKind.False, $"{path} deveria ser boolean.");
        }
    }
}
