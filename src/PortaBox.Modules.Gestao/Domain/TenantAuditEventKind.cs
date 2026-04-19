namespace PortaBox.Modules.Gestao.Domain;

public enum TenantAuditEventKind : short
{
    Created = 1,
    Activated = 2,
    MagicLinkResent = 3,
    Other = 4
}
