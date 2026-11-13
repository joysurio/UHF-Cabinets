namespace UHFCabinets.Hardware;

public sealed class StubDoorModule : IDoorModule
{
    public Task RequestOpenDoorAsync(CancellationToken cancellationToken = default) =>
        Task.FromException(new InvalidOperationException(
            "Door hardware is not configured. Use testMode: true to simulate, or set hardware.mode to httpBridge with hardware.bridgeBaseUrl for a local hardware service."));

    public Task WaitForDoorClosedAsync(CancellationToken cancellationToken = default) =>
        Task.FromException(new InvalidOperationException(
            "Door hardware is not configured. Use testMode: true to simulate, or set hardware.mode to httpBridge with hardware.bridgeBaseUrl for a local hardware service."));
}
