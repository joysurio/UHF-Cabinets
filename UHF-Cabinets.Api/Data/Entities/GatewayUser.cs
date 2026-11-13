using UHFCabinets.Api.Data;

namespace UHFCabinets.Api.Data.Entities;

public class GatewayUser
{
    public string IdUsuario { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public string? Rol { get; set; }
    public SyncStatus SyncStatus { get; set; } = SyncStatus.Pending;
    public string? SyncLastError { get; set; }
}
