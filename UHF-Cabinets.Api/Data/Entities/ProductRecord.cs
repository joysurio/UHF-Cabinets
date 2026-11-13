using UHFCabinets.Api.Data;

namespace UHFCabinets.Api.Data.Entities;

public class ProductRecord
{
    public string IdProducto { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public SyncStatus SyncStatus { get; set; } = SyncStatus.Pending;
    public string? SyncLastError { get; set; }
}
