using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UHFCabinets.Api.Configuration;
using UHFCabinets.Api.Data;
using SyncStatusEntity = UHFCabinets.Api.Data.SyncStatus;

namespace UHFCabinets.Api.Services;

public class OutboundSyncProcessorHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly LfsProductionOptions _options;
    private readonly ILogger<OutboundSyncProcessorHostedService> _logger;

    public OutboundSyncProcessorHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<LfsProductionOptions> options,
        ILogger<OutboundSyncProcessorHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbound sync batch failed");
            }

            var delay = TimeSpan.FromSeconds(Math.Clamp(_options.PollSeconds, 5, 300));
            await Task.Delay(delay, stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApiDbContext>();
        var lfs = scope.ServiceProvider.GetRequiredService<ILfsOutboundSyncService>();

        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            await MarkAllPendingAsSyncedLocallyAsync(db, ct);
            return;
        }

        var movements = await db.Movements
            .Where(m => m.SyncStatus == SyncStatusEntity.Pending || m.SyncStatus == SyncStatusEntity.Error || m.SyncStatus == SyncStatusEntity.Retrying)
            .OrderBy(m => m.Id)
            .Take(50)
            .ToListAsync(ct);

        foreach (var m in movements)
        {
            m.SyncStatus = SyncStatusEntity.Retrying;
            await db.SaveChangesAsync(ct);

            var (ok, err) = await lfs.TryPushMovementAsync(m, ct);
            if (ok)
            {
                m.SyncStatus = SyncStatusEntity.Synced;
                m.SyncLastError = null;
            }
            else
            {
                m.SyncStatus = SyncStatusEntity.Error;
                m.SyncLastError = err;
            }

            await db.SaveChangesAsync(ct);
        }

        var items = await db.RfidItems
            .Where(x => x.SyncStatus == SyncStatusEntity.Pending || x.SyncStatus == SyncStatusEntity.Error || x.SyncStatus == SyncStatusEntity.Retrying)
            .OrderBy(x => x.Rfid)
            .Take(50)
            .ToListAsync(ct);

        foreach (var item in items)
        {
            item.SyncStatus = SyncStatusEntity.Retrying;
            await db.SaveChangesAsync(ct);

            var (ok, err) = await lfs.TryPushRfidItemAsync(item, ct);
            if (ok)
            {
                item.SyncStatus = SyncStatusEntity.Synced;
                item.SyncLastError = null;
            }
            else
            {
                item.SyncStatus = SyncStatusEntity.Error;
                item.SyncLastError = err;
            }

            await db.SaveChangesAsync(ct);
        }
    }

    private static async Task MarkAllPendingAsSyncedLocallyAsync(ApiDbContext db, CancellationToken ct)
    {
        var pendingMovements = await db.Movements
            .Where(m => m.SyncStatus == SyncStatusEntity.Pending || m.SyncStatus == SyncStatusEntity.Retrying)
            .ToListAsync(ct);
        foreach (var m in pendingMovements)
        {
            m.SyncStatus = SyncStatusEntity.Synced;
            m.SyncLastError = null;
        }

        var pendingItems = await db.RfidItems
            .Where(x => x.SyncStatus == SyncStatusEntity.Pending || x.SyncStatus == SyncStatusEntity.Retrying)
            .ToListAsync(ct);
        foreach (var x in pendingItems)
        {
            x.SyncStatus = SyncStatusEntity.Synced;
            x.SyncLastError = null;
        }

        if (pendingMovements.Count > 0 || pendingItems.Count > 0)
            await db.SaveChangesAsync(ct);
    }
}
