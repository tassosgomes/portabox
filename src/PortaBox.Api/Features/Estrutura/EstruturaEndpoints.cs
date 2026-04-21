using System.Security.Claims;
using FluentValidation;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.OpenApi;
using PortaBox.Api.Extensions;
using PortaBox.Application.Abstractions.Messaging;
using PortaBox.Application.Abstractions.MultiTenancy;
using PortaBox.Modules.Gestao.Application.Blocos;
using PortaBox.Modules.Gestao.Application.Estrutura;
using PortaBox.Modules.Gestao.Application.Unidades;

namespace PortaBox.Api.Features.Estrutura;

public static class EstruturaEndpoints
{
    public static IEndpointRouteBuilder MapEstruturaEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/condominios/{condominioId:guid}/estrutura", async (
                Guid condominioId,
                IQueryHandler<GetEstruturaQuery, EstruturaDto> handler,
                HttpContext httpContext,
                CancellationToken cancellationToken,
                bool includeInactive = false) =>
            {
                if (!IsSindicoTenantAuthorized(httpContext, condominioId))
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status403Forbidden,
                        title: "Acesso negado",
                        type: "https://portabox.app/problems/forbidden",
                        detail: "Você não tem permissão para executar esta operação",
                        instance: httpContext.Request.Path);
                }

                var result = await handler.HandleAsync(new GetEstruturaQuery(condominioId, includeInactive), cancellationToken);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemHttpResult(httpContext);
            })
            .RequireAuthorization(AuthorizationPolicies.RequireSindico)
            .WithName("getEstrutura")
            .WithSummary("Ler árvore de estrutura (síndico)")
            .WithTags("Estrutura")
            .WithOpenApi(operation => WithDefaultProblemResponses(operation, "getEstrutura", ["Estrutura"]));

        endpoints.MapPost("/condominios/{condominioId:guid}/blocos", async (
                Guid condominioId,
                CreateBlocoRequest request,
                ICommandHandler<CreateBlocoCommand, BlocoDto> handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    if (!IsSindicoTenantAuthorized(httpContext, condominioId))
                    {
                        return Results.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Acesso negado",
                            type: "https://portabox.app/problems/forbidden",
                            detail: "Você não tem permissão para executar esta operação",
                            instance: httpContext.Request.Path);
                    }

                    var result = await handler.HandleAsync(
                        new CreateBlocoCommand(condominioId, request.Nome, GetUserId(httpContext)),
                        cancellationToken);

                    if (!result.IsSuccess)
                    {
                        return result.ToProblemHttpResult(httpContext);
                    }

                    var bloco = result.Value ?? throw new InvalidOperationException("Successful bloco result must contain a value.");
                    return bloco.ToCreatedHttpResult($"/api/v1/condominios/{condominioId}/blocos/{bloco.Id}");
                }
                catch (ValidationException exception)
                {
                    return exception.ToValidationProblemHttpResult(httpContext);
                }
            })
            .RequireAuthorization(AuthorizationPolicies.RequireSindico)
            .Produces<BlocoDto>(StatusCodes.Status201Created)
            .WithName("criarBloco")
            .WithSummary("Criar bloco")
            .WithTags("Blocos")
            .WithOpenApi(operation => WithDefaultProblemResponses(operation, "criarBloco", ["Blocos"], createdResponse: true));

        endpoints.MapPatch("/condominios/{condominioId:guid}/blocos/{blocoId:guid}", async (
                Guid condominioId,
                Guid blocoId,
                RenameBlocoRequest request,
                ICommandHandler<RenameBlocoCommand, BlocoDto> handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    if (!IsSindicoTenantAuthorized(httpContext, condominioId))
                    {
                        return Results.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Acesso negado",
                            type: "https://portabox.app/problems/forbidden",
                            detail: "Você não tem permissão para executar esta operação",
                            instance: httpContext.Request.Path);
                    }

                    var result = await handler.HandleAsync(
                        new RenameBlocoCommand(condominioId, blocoId, request.Nome, GetUserId(httpContext)),
                        cancellationToken);

                    return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemHttpResult(httpContext);
                }
                catch (ValidationException exception)
                {
                    return exception.ToValidationProblemHttpResult(httpContext);
                }
            })
            .RequireAuthorization(AuthorizationPolicies.RequireSindico)
            .WithName("renomearBloco")
            .WithSummary("Renomear bloco")
            .WithTags("Blocos")
            .WithOpenApi(operation => WithDefaultProblemResponses(operation, "renomearBloco", ["Blocos"]));

        endpoints.MapPost("/condominios/{condominioId:guid}/blocos/{blocoId:guid}:inativar", async (
                Guid condominioId,
                Guid blocoId,
                ICommandHandler<InativarBlocoCommand, BlocoDto> handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    if (!IsSindicoTenantAuthorized(httpContext, condominioId))
                    {
                        return Results.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Acesso negado",
                            type: "https://portabox.app/problems/forbidden",
                            detail: "Você não tem permissão para executar esta operação",
                            instance: httpContext.Request.Path);
                    }

                    var result = await handler.HandleAsync(
                        new InativarBlocoCommand(condominioId, blocoId, GetUserId(httpContext)),
                        cancellationToken);

                    return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemHttpResult(httpContext);
                }
                catch (ValidationException exception)
                {
                    return exception.ToValidationProblemHttpResult(httpContext);
                }
            })
            .RequireAuthorization(AuthorizationPolicies.RequireSindico)
            .WithName("inativarBloco")
            .WithSummary("Inativar bloco")
            .WithTags("Blocos")
            .WithOpenApi(operation => WithDefaultProblemResponses(operation, "inativarBloco", ["Blocos"]));

        endpoints.MapPost("/condominios/{condominioId:guid}/blocos/{blocoId:guid}:reativar", async (
                Guid condominioId,
                Guid blocoId,
                ICommandHandler<ReativarBlocoCommand, BlocoDto> handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    if (!IsSindicoTenantAuthorized(httpContext, condominioId))
                    {
                        return Results.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Acesso negado",
                            type: "https://portabox.app/problems/forbidden",
                            detail: "Você não tem permissão para executar esta operação",
                            instance: httpContext.Request.Path);
                    }

                    var result = await handler.HandleAsync(
                        new ReativarBlocoCommand(condominioId, blocoId, GetUserId(httpContext)),
                        cancellationToken);

                    return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemHttpResult(httpContext);
                }
                catch (ValidationException exception)
                {
                    return exception.ToValidationProblemHttpResult(httpContext);
                }
            })
            .RequireAuthorization(AuthorizationPolicies.RequireSindico)
            .WithName("reativarBloco")
            .WithSummary("Reativar bloco")
            .WithTags("Blocos")
            .WithOpenApi(operation => WithDefaultProblemResponses(operation, "reativarBloco", ["Blocos"]));

        endpoints.MapPost("/condominios/{condominioId:guid}/blocos/{blocoId:guid}/unidades", async (
                Guid condominioId,
                Guid blocoId,
                CreateUnidadeRequest request,
                ICommandHandler<CreateUnidadeCommand, UnidadeDto> handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    if (!IsSindicoTenantAuthorized(httpContext, condominioId))
                    {
                        return Results.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Acesso negado",
                            type: "https://portabox.app/problems/forbidden",
                            detail: "Você não tem permissão para executar esta operação",
                            instance: httpContext.Request.Path);
                    }

                    var result = await handler.HandleAsync(
                        new CreateUnidadeCommand(condominioId, blocoId, request.Andar, request.Numero, GetUserId(httpContext)),
                        cancellationToken);

                    if (!result.IsSuccess)
                    {
                        return result.ToProblemHttpResult(httpContext);
                    }

                    var unidade = result.Value ?? throw new InvalidOperationException("Successful unidade result must contain a value.");
                    return unidade.ToCreatedHttpResult($"/api/v1/condominios/{condominioId}/blocos/{blocoId}/unidades/{unidade.Id}");
                }
                catch (ValidationException exception)
                {
                    return exception.ToValidationProblemHttpResult(httpContext);
                }
            })
            .RequireAuthorization(AuthorizationPolicies.RequireSindico)
            .Produces<UnidadeDto>(StatusCodes.Status201Created)
            .WithName("criarUnidade")
            .WithSummary("Criar unidade")
            .WithTags("Unidades")
            .WithOpenApi(operation => WithDefaultProblemResponses(operation, "criarUnidade", ["Unidades"], createdResponse: true));

        endpoints.MapPost("/condominios/{condominioId:guid}/blocos/{blocoId:guid}/unidades/{unidadeId:guid}:inativar", async (
                Guid condominioId,
                Guid blocoId,
                Guid unidadeId,
                ICommandHandler<InativarUnidadeCommand, UnidadeDto> handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    if (!IsSindicoTenantAuthorized(httpContext, condominioId))
                    {
                        return Results.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Acesso negado",
                            type: "https://portabox.app/problems/forbidden",
                            detail: "Você não tem permissão para executar esta operação",
                            instance: httpContext.Request.Path);
                    }

                    var result = await handler.HandleAsync(
                        new InativarUnidadeCommand(condominioId, blocoId, unidadeId, GetUserId(httpContext)),
                        cancellationToken);

                    return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemHttpResult(httpContext);
                }
                catch (ValidationException exception)
                {
                    return exception.ToValidationProblemHttpResult(httpContext);
                }
            })
            .RequireAuthorization(AuthorizationPolicies.RequireSindico)
            .WithName("inativarUnidade")
            .WithSummary("Inativar unidade")
            .WithTags("Unidades")
            .WithOpenApi(operation => WithDefaultProblemResponses(operation, "inativarUnidade", ["Unidades"]));

        endpoints.MapPost("/condominios/{condominioId:guid}/blocos/{blocoId:guid}/unidades/{unidadeId:guid}:reativar", async (
                Guid condominioId,
                Guid blocoId,
                Guid unidadeId,
                ICommandHandler<ReativarUnidadeCommand, UnidadeDto> handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    if (!IsSindicoTenantAuthorized(httpContext, condominioId))
                    {
                        return Results.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Acesso negado",
                            type: "https://portabox.app/problems/forbidden",
                            detail: "Você não tem permissão para executar esta operação",
                            instance: httpContext.Request.Path);
                    }

                    var result = await handler.HandleAsync(
                        new ReativarUnidadeCommand(condominioId, blocoId, unidadeId, GetUserId(httpContext)),
                        cancellationToken);

                    return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemHttpResult(httpContext);
                }
                catch (ValidationException exception)
                {
                    return exception.ToValidationProblemHttpResult(httpContext);
                }
            })
            .RequireAuthorization(AuthorizationPolicies.RequireSindico)
            .WithName("reativarUnidade")
            .WithSummary("Reativar unidade")
            .WithTags("Unidades")
            .WithOpenApi(operation => WithDefaultProblemResponses(operation, "reativarUnidade", ["Unidades"]));

        endpoints.MapGet("/admin/condominios/{condominioId:guid}/estrutura", async (
                Guid condominioId,
                ITenantContext tenantContext,
                IQueryHandler<GetEstruturaQuery, EstruturaDto> handler,
                HttpContext httpContext,
                CancellationToken cancellationToken,
                bool includeInactive = false) =>
            {
                using var scope = tenantContext.BeginScope(condominioId);

                var result = await handler.HandleAsync(new GetEstruturaQuery(condominioId, includeInactive), cancellationToken);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemHttpResult(httpContext);
            })
            .RequireAuthorization(AuthorizationPolicies.RequireOperator)
            .WithName("getEstruturaAdmin")
            .WithSummary("Ler estrutura de qualquer tenant (operador)")
            .WithTags("Admin", "Estrutura")
            .WithOpenApi(operation => WithDefaultProblemResponses(operation, "getEstruturaAdmin", ["Admin", "Estrutura"]));

        return endpoints;
    }

    private static Guid GetUserId(HttpContext httpContext)
    {
        var value = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Authenticated user has no NameIdentifier claim.");

        return Guid.Parse(value);
    }

    private static bool IsSindicoTenantAuthorized(HttpContext httpContext, Guid condominioId)
    {
        var tenantClaim = httpContext.User.FindFirst("tenant_id")?.Value;

        return Guid.TryParse(tenantClaim, out var tenantId) && tenantId == condominioId;
    }

    private static OpenApiOperation WithDefaultProblemResponses(
        OpenApiOperation operation,
        string operationId,
        string[] tags,
        bool createdResponse = false)
    {
        operation.OperationId = operationId;
        operation.Tags = tags.Select(tag => new OpenApiTag { Name = tag }).ToList();

        operation.Responses.TryAdd("401", CreateProblemResponse("Não autenticado"));
        operation.Responses.TryAdd("403", CreateProblemResponse("Autenticado mas sem permissão para a operação"));
        operation.Responses.TryAdd("404", CreateProblemResponse("Recurso não encontrado"));
        operation.Responses.TryAdd("500", CreateProblemResponse("Erro interno inesperado"));

        if (createdResponse)
        {
            operation.Responses.TryAdd("400", CreateValidationProblemResponse());
            operation.Responses.TryAdd("409", CreateProblemResponse("Conflito de estado"));
        }

        if (operationId is "renomearBloco" or "criarUnidade")
        {
            operation.Responses.TryAdd("400", CreateValidationProblemResponse());
        }

        if (operationId is "renomearBloco" or "reativarBloco" or "criarUnidade" or "reativarUnidade")
        {
            operation.Responses.TryAdd("409", CreateProblemResponse("Conflito de estado"));
        }

        if (operationId is "renomearBloco" or "inativarBloco" or "reativarBloco" or "criarUnidade" or "inativarUnidade" or "reativarUnidade")
        {
            operation.Responses.TryAdd("422", CreateProblemResponse("Requisição bem formada mas impossível de processar"));
        }

        return operation;
    }

    private static OpenApiResponse CreateProblemResponse(string description)
    {
        return new OpenApiResponse
        {
            Description = description,
            Content =
            {
                ["application/problem+json"] = new OpenApiMediaType()
            }
        };
    }

    private static OpenApiResponse CreateValidationProblemResponse()
    {
        return new OpenApiResponse
        {
            Description = "Requisição inválida — erro de validação de campos",
            Content =
            {
                ["application/problem+json"] = new OpenApiMediaType()
            }
        };
    }
}
