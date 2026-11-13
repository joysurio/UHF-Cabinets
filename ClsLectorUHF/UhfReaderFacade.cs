using ClsLectorChainway;

namespace ClsLectorUHF;

/// <summary>
/// Brand-agnostic UHF façade used by the cabinet app. For Chainway, composes <see cref="IChainwayTagSource"/>.
/// Add CAEN / Promag branches here as additional brands.
/// </summary>
public sealed class UhfReaderFacade
{
    private readonly IChainwayTagSource _chainway;

    public UhfReaderFacade(IChainwayTagSource chainway) =>
        _chainway = chainway ?? throw new ArgumentNullException(nameof(chainway));

    /// <summary>Constructs the default stack for the given brand (Chainway → placeholder until vendor code is merged).</summary>
    public UhfReaderFacade(UhfHardwareBrand brand)
    {
        _chainway = brand switch
        {
            UhfHardwareBrand.Chainway => new ChainwayPlaceholderTagSource(),
            _ => throw new NotSupportedException($"UHF brand '{brand}' is not implemented yet.")
        };
    }

    public Task<IReadOnlyList<string>> ReadTagsAsync(CancellationToken cancellationToken = default) =>
        _chainway.ReadTagsAsync(cancellationToken);
}
