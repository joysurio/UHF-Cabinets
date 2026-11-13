namespace UHFCabinets.Hardware;

public interface IDoorModule
{
    Task RequestOpenDoorAsync(CancellationToken cancellationToken = default);
    Task WaitForDoorClosedAsync(CancellationToken cancellationToken = default);
}
