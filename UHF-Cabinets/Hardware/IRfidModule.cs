namespace UHFCabinets.Hardware;

public interface IRfidModule
{
    Task<IReadOnlyList<string>> ReadTagsAsync(CancellationToken cancellationToken = default);
}
