namespace UHFCabinets.Hardware;

public sealed class StubRfidModule : IRfidModule
{
    public Task<IReadOnlyList<string>> ReadTagsAsync(CancellationToken cancellationToken = default) =>
        Task.FromException<IReadOnlyList<string>>(new InvalidOperationException(
            "RFID reader is not configured. Use testMode: true to enter tags manually, or hardware.mode httpBridge with hardware.bridgeBaseUrl for a local reader service."));
}
