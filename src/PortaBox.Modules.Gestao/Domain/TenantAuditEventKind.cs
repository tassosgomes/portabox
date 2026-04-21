namespace PortaBox.Modules.Gestao.Domain;

public enum TenantAuditEventKind : short
{
    Created = 1,
    Activated = 2,
    MagicLinkResent = 3,
    Other = 4,
    BlocoCriado = 5,
    BlocoRenomeado = 6,
    BlocoInativado = 7,
    BlocoReativado = 8,
    UnidadeCriada = 9,
    UnidadeInativada = 10,
    UnidadeReativada = 11
}
