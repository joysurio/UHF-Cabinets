using UHFCabinets.Api.Data;

namespace UHFCabinets.Api.Data.Entities;

public class MovementRecord
{
    public long Id { get; set; }
    public string IdMov { get; set; } = string.Empty;
    public string Rfid { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public string TipoOperacion { get; set; } = string.Empty;
    public DateTimeOffset Fecha { get; set; }
    public string CabinetId { get; set; } = string.Empty;
    public SyncStatus SyncStatus { get; set; } = SyncStatus.Pending;
    public string? SyncLastError { get; set; }
    public string? TraceId { get; set; }
}
