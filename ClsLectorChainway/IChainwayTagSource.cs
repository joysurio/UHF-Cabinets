namespace ClsLectorChainway;

/// <summary>Chainway-specific tag acquisition. Replace the default implementation with vendor SDK calls from clsLectorChainway.rar.</summary>
public interface IChainwayTagSource
{
    Task<IReadOnlyList<string>> ReadTagsAsync(CancellationToken cancellationToken = default);
}
