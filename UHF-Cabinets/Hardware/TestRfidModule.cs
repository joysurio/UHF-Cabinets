namespace UHFCabinets.Hardware;

public sealed class TestRfidModule : IRfidModule
{
    private IReadOnlyList<string> _buffer = Array.Empty<string>();

    public void SetSimulatedTags(IReadOnlyList<string> tags) =>
        _buffer = tags.Select(t => t.Trim()).Where(t => t.Length > 0).Distinct(StringComparer.Ordinal).ToList();

    public Task<IReadOnlyList<string>> ReadTagsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_buffer);
}
