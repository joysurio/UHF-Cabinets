namespace ClsLectorChainway;

/// <summary>
/// Placeholder until the real Chainway reader code from the client archive is merged here.
/// </summary>
public sealed class ChainwayPlaceholderTagSource : IChainwayTagSource
{
    public Task<IReadOnlyList<string>> ReadTagsAsync(CancellationToken cancellationToken = default) =>
        Task.FromException<IReadOnlyList<string>>(new InvalidOperationException(
            "Chainway reader is not wired: replace ChainwayPlaceholderTagSource with the integration from clsLectorChainway.rar (USB/COM/SDK)."));
}
