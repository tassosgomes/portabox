using System.Security.Claims;
using PortaBox.Api.Extensions;
using PortaBox.Application.Abstractions.Messaging;
using PortaBox.Application.Abstractions.MultiTenancy;
using PortaBox.Application.Abstractions.Storage;
using PortaBox.Modules.Gestao.Application.Commands.ActivateCondominio;
using PortaBox.Modules.Gestao.Application.Commands.CreateCondominio;
using PortaBox.Modules.Gestao.Application.Commands.ResendMagicLink;
using PortaBox.Modules.Gestao.Application.Commands.UploadOptInDocument;
using PortaBox.Modules.Gestao.Application.Common;
using PortaBox.Modules.Gestao.Application.Queries.GetCondominioDetails;
using PortaBox.Modules.Gestao.Application.Queries.ListCondominios;
using PortaBox.Modules.Gestao.Application.Repositories;
using PortaBox.Modules.Gestao.Domain;

namespace PortaBox.Api.Endpoints;

public static class CondominiosEndpoints
{
    public static IEndpointRouteBuilder MapCondominiosEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/admin/condominios")
            .RequireAuthorization(AuthorizationPolicies.RequireOperator)
            .WithTags("Admin - Condominios");

        group.MapPost("/", async (
            CreateCondominioRequest request,
            ICommandHandler<CreateCondominioCommand, CreateCondominioResult> handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(httpContext);

            var command = new CreateCondominioCommand(
                userId,
                request.NomeFantasia,
                request.Cnpj,
                request.EnderecoLogradouro,
                request.EnderecoNumero,
                request.EnderecoComplemento,
                request.EnderecoBairro,
                request.EnderecoCidade,
                request.EnderecoUf,
                request.EnderecoCep,
                request.AdministradoraNome,
                request.OptIn.DataAssembleia,
                request.OptIn.QuorumDescricao,
                request.OptIn.SignatarioNome,
                request.OptIn.SignatarioCpf,
                request.OptIn.DataTermo,
                request.Sindico.Nome,
                request.Sindico.Email,
                request.Sindico.CelularE164);

            var result = await handler.HandleAsync(command, cancellationToken);

            if (!result.IsSuccess)
            {
                return result.Error switch
                {
                    CreateCondominioErrors.CnpjAlreadyExists => Results.Problem(
                        title: "CNPJ já cadastrado.",
                        detail: result.Error,
                        statusCode: StatusCodes.Status409Conflict),
                    CreateCondominioErrors.SindicoEmailAlreadyExists => Results.Problem(
                        title: "E-mail do síndico já cadastrado.",
                        detail: result.Error,
                        statusCode: StatusCodes.Status409Conflict),
                    _ => Results.Problem(
                        title: "Erro ao criar condomínio.",
                        detail: result.Error,
                        statusCode: StatusCodes.Status422UnprocessableEntity)
                };
            }

            var response = new CreateCondominioResponse(result.Value!.CondominioId, result.Value.SindicoUserId);
            return Results.Created($"/api/v1/admin/condominios/{result.Value.CondominioId}", response);
        });

        group.MapGet("/", async (
            IQueryHandler<ListCondominiosQuery, PagedResult<CondominioListItemDto>> handler,
            int page = 1,
            int pageSize = 20,
            CondominioStatus? status = null,
            string? q = null,
            CancellationToken cancellationToken = default) =>
        {
            var result = await handler.HandleAsync(new ListCondominiosQuery(page, pageSize, status, q), cancellationToken);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(statusCode: StatusCodes.Status500InternalServerError);
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            IQueryHandler<GetCondominioDetailsQuery, CondominioDetailsDto> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(new GetCondominioDetailsQuery(id), cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound();
        });

        group.MapPost("/{id:guid}:activate", async (
            Guid id,
            ActivateCondominioRequest request,
            ICommandHandler<ActivateCondominioCommand, ActivateCondominioResult> handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(httpContext);
            var result = await handler.HandleAsync(
                new ActivateCondominioCommand(id, userId, request.Note),
                cancellationToken);

            if (!result.IsSuccess)
            {
                return result.Error switch
                {
                    ActivateCondominioErrors.NotFound => Results.NotFound(),
                    ActivateCondominioErrors.AlreadyActive => Results.Problem(
                        title: "Condomínio já está ativo.",
                        detail: result.Error,
                        statusCode: StatusCodes.Status409Conflict),
                    _ => Results.Problem(statusCode: StatusCodes.Status422UnprocessableEntity)
                };
            }

            return Results.Ok(new ActivateCondominioResponse(result.Value!.CondominioId));
        });

        group.MapPost("/{id:guid}/sindicos/{sindicoUserId:guid}:resend-magic-link", async (
            Guid id,
            Guid sindicoUserId,
            ICommandHandler<ResendMagicLinkCommand, ResendMagicLinkResult> handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var performedBy = GetUserId(httpContext);
            var result = await handler.HandleAsync(
                new ResendMagicLinkCommand(id, sindicoUserId, performedBy),
                cancellationToken);

            if (!result.IsSuccess)
            {
                return result.Error switch
                {
                    ResendMagicLinkErrors.NotFound => Results.NotFound(),
                    ResendMagicLinkErrors.RateLimited => Results.Problem(
                        title: "Taxa limite atingida. Tente novamente mais tarde.",
                        statusCode: StatusCodes.Status429TooManyRequests),
                    ResendMagicLinkErrors.AlreadyHasPassword => Results.Problem(
                        title: "Síndico já definiu a senha.",
                        detail: result.Error,
                        statusCode: StatusCodes.Status409Conflict),
                    _ => Results.Problem(statusCode: StatusCodes.Status422UnprocessableEntity)
                };
            }

            return Results.Ok(new ResendMagicLinkResponse(result.Value!.SindicoUserId));
        });

        group.MapPost("/{id:guid}/opt-in-documents", async (
            Guid id,
            IFormFile file,
            ICommandHandler<UploadOptInDocumentCommand, UploadOptInDocumentResult> handler,
            HttpContext httpContext,
            string? kind = null,
            CancellationToken cancellationToken = default) =>
        {
            var userId = GetUserId(httpContext);
            var documentKind = Enum.TryParse<OptInDocumentKind>(kind, ignoreCase: true, out var parsedKind)
                ? parsedKind
                : OptInDocumentKind.Outro;

            await using var stream = file.OpenReadStream();

            var command = new UploadOptInDocumentCommand(
                id,
                documentKind,
                file.ContentType,
                file.FileName,
                file.Length,
                stream,
                userId);

            var result = await handler.HandleAsync(command, cancellationToken);

            if (!result.IsSuccess)
            {
                return result.Error switch
                {
                    UploadOptInDocumentErrors.TenantNotFound => Results.NotFound(),
                    _ => Results.Problem(
                        title: "Não foi possível fazer o upload do documento.",
                        detail: result.Error,
                        statusCode: StatusCodes.Status422UnprocessableEntity)
                };
            }

            return Results.Created(
                $"/api/v1/admin/condominios/{id}/opt-in-documents/{result.Value!.DocumentId}:download",
                new UploadOptInDocumentResponse(result.Value.DocumentId));
        }).DisableAntiforgery();

        group.MapGet("/{id:guid}/opt-in-documents/{docId:guid}:download", async (
            Guid id,
            Guid docId,
            ITenantContext tenantContext,
            IOptInDocumentRepository optInDocumentRepository,
            IObjectStorage objectStorage,
            CancellationToken cancellationToken) =>
        {
            using var _ = tenantContext.BeginScope(id);

            var document = await optInDocumentRepository.GetByIdAsync(docId, cancellationToken);

            if (document is null)
            {
                return Results.NotFound();
            }

            var url = await objectStorage.GetDownloadUrlAsync(
                document.StorageKey,
                TimeSpan.FromMinutes(5),
                cancellationToken);

            var expiresAt = DateTimeOffset.UtcNow.AddMinutes(5);

            return Results.Ok(new DownloadUrlResponse(url.ToString(), expiresAt));
        });

        return routes;
    }

    private static Guid GetUserId(HttpContext httpContext)
    {
        var value = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Authenticated user has no NameIdentifier claim.");

        return Guid.Parse(value);
    }

    public sealed record CreateCondominioRequest(
        string NomeFantasia,
        string Cnpj,
        string? EnderecoLogradouro,
        string? EnderecoNumero,
        string? EnderecoComplemento,
        string? EnderecoBairro,
        string? EnderecoCidade,
        string? EnderecoUf,
        string? EnderecoCep,
        string? AdministradoraNome,
        CreateCondominioOptInRequest OptIn,
        CreateCondominioSindicoRequest Sindico);

    public sealed record CreateCondominioOptInRequest(
        DateOnly DataAssembleia,
        string QuorumDescricao,
        string SignatarioNome,
        string SignatarioCpf,
        DateOnly DataTermo);

    public sealed record CreateCondominioSindicoRequest(
        string Nome,
        string Email,
        string CelularE164);

    public sealed record CreateCondominioResponse(Guid CondominioId, Guid SindicoUserId);

    public sealed record ActivateCondominioRequest(string? Note);

    public sealed record ActivateCondominioResponse(Guid CondominioId);

    public sealed record ResendMagicLinkResponse(Guid SindicoUserId);

    public sealed record UploadOptInDocumentResponse(Guid DocumentId);

    public sealed record DownloadUrlResponse(string Url, DateTimeOffset ExpiresAt);
}
