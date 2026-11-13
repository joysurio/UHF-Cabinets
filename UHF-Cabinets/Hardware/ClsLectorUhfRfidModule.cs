using ClsLectorUHF;

namespace UHFCabinets.Hardware;

/// <summary>Cabinet app adapter: <c>UHF-Cabinets</c> → <see cref="UhfReaderFacade"/> (ClsLectorUHF) → Chainway.</summary>
public sealed class ClsLectorUhfRfidModule : IRfidModule
{
    private readonly UhfReaderFacade _facade;

    public ClsLectorUhfRfidModule(UhfReaderFacade facade) =>
        _facade = facade ?? throw new ArgumentNullException(nameof(facade));

    public async Task<IReadOnlyList<string>> ReadTagsAsync(CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(60));
        return await _facade.ReadTagsAsync(cts.Token).ConfigureAwait(false);
    }
}
