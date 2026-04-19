using PortaBox.Application.Abstractions.Messaging;

namespace PortaBox.Modules.Gestao.Application.Commands.CreateCondominio;

public sealed record CreateCondominioCommand(
    Guid CreatedByUserId,
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
    DateOnly DataAssembleia,
    string QuorumDescricao,
    string SignatarioNome,
    string SignatarioCpf,
    DateOnly DataTermo,
    string SindicoNomeCompleto,
    string SindicoEmail,
    string SindicoCelularE164) : ICommand<CreateCondominioResult>;
