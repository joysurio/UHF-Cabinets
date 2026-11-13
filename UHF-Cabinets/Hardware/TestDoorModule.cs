namespace UHFCabinets.Hardware;

public sealed class TestDoorModule : IDoorModule
{
    private TaskCompletionSource<bool>? _closed;

    public Task RequestOpenDoorAsync(CancellationToken cancellationToken = default)
    {
        _closed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        return Task.CompletedTask;
    }

    public Task WaitForDoorClosedAsync(CancellationToken cancellationToken = default)
    {
        if (_closed == null)
            _closed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        cancellationToken.Register(() => _closed.TrySetCanceled(cancellationToken));
        return _closed.Task;
    }

    public void SimulateDoorClosed()
    {
        _closed?.TrySetResult(true);
    }
}
