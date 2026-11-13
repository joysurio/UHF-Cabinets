using UHFCabinets.Api.Data.Entities;

namespace UHFCabinets.Api.Services;

public interface ILfsOutboundSyncService
{
    Task<(bool Ok, string? Error)> TryPushMovementAsync(MovementRecord movement, CancellationToken cancellationToken = default);
    Task<(bool Ok, string? Error)> TryPushRfidItemAsync(RfidItemRecord item, CancellationToken cancellationToken = default);
}
