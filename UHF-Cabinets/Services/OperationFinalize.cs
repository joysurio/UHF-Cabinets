using UHFCabinets.Data.Services;
using UHFCabinets.Session;

namespace UHFCabinets.Services;

public static class OperationFinalize
{
    public static async Task CompleteAsync(
        LocalCabinetService local,
        CabinetGatewayClient gateway,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(OperationContext.IdMov) || string.IsNullOrEmpty(AppSession.UserId))
            throw new InvalidOperationException("Invalid operation state.");

        var idMov = OperationContext.IdMov;
        var user = AppSession.UserId;
        var now = DateTimeOffset.UtcNow;

        foreach (var r in OperationContext.Removed.Distinct(StringComparer.Ordinal))
        {
            local.AddMovimiento(idMov, r, user, "SALIDA", now);
            var ok = await gateway.TryPostMovementAsync(idMov, r, user, "SALIDA", now, ct);
            if (!ok)
            {
                local.EnqueueSync(LocalCabinetService.SyncKindMovement, new
                {
                    IdMov = idMov,
                    Rfid = r,
                    Usuario = user,
                    TipoOperacion = "SALIDA",
                    Fecha = now.ToString("O")
                });
            }
        }

        foreach (var a in OperationContext.Added.Distinct(StringComparer.Ordinal))
        {
            local.AddMovimiento(idMov, a, user, "ENTRADA", now);
            var ok = await gateway.TryPostMovementAsync(idMov, a, user, "ENTRADA", now, ct);
            if (!ok)
            {
                local.EnqueueSync(LocalCabinetService.SyncKindMovement, new
                {
                    IdMov = idMov,
                    Rfid = a,
                    Usuario = user,
                    TipoOperacion = "ENTRADA",
                    Fecha = now.ToString("O")
                });
            }
        }

        local.SetLastScanRfids(OperationContext.AfterSnapshot);
        OperationContext.SummarySalidas = OperationContext.Removed.Count;
        OperationContext.SummaryEntradas = OperationContext.Added.Count;
    }
}
