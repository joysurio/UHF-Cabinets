using System.Text.Json;
using UHFCabinets.Data.Configuration;
using UHFCabinets.Data.Services;

namespace UHFCabinets.Services;

public class OutboundSyncService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private readonly LocalCabinetService _local;
    private readonly CabinetGatewayClient _gateway;

    public OutboundSyncService(LocalCabinetService local, CabinetGatewayClient gateway)
    {
        _local = local;
        _gateway = gateway;
    }

    public async Task FlushPendingAsync(CancellationToken ct = default)
    {
        foreach (var row in _local.GetPendingSync().ToList())
        {
            try
            {
                if (row.Kind == LocalCabinetService.SyncKindMovement)
                {
                    var p = JsonSerializer.Deserialize<MovementPayload>(row.PayloadJson, JsonOpts);
                    if (p == null) { _local.RemovePending(row.Id); continue; }
                    var ok = await _gateway.TryPostMovementAsync(
                        p.IdMov, p.Rfid, p.Usuario, p.TipoOperacion,
                        DateTimeOffset.Parse(p.Fecha, null, System.Globalization.DateTimeStyles.RoundtripKind), ct);
                    if (ok) _local.RemovePending(row.Id);
                }
                else if (row.Kind == LocalCabinetService.SyncKindRfid)
                {
                    var p = JsonSerializer.Deserialize<RfidPayload>(row.PayloadJson, JsonOpts);
                    if (p == null) { _local.RemovePending(row.Id); continue; }
                    var ok = await _gateway.TryRegisterRfidAsync(p.Rfid, p.IdProducto, ct);
                    if (ok) _local.RemovePending(row.Id);
                }
                else
                    _local.RemovePending(row.Id);
            }
            catch
            {
                // keep row for retry
            }
        }
    }

    private sealed class MovementPayload
    {
        public string IdMov { get; set; } = "";
        public string Rfid { get; set; } = "";
        public string Usuario { get; set; } = "";
        public string TipoOperacion { get; set; } = "";
        public string Fecha { get; set; } = "";
    }

    private sealed class RfidPayload
    {
        public string Rfid { get; set; } = "";
        public string IdProducto { get; set; } = "";
    }
}

