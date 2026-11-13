using UHFCabinets.Api.Data;

namespace UHFCabinets.Api.Data.Entities;

public class RfidItemRecord
{
    public string Rfid { get; set; } = string.Empty;
    public string IdProducto { get; set; } = string.Empty;
    public SyncStatus SyncStatus { get; set; } = SyncStatus.Pending;
    public string? SyncLastError { get; set; }
    public ProductRecord? Product { get; set; }
}
