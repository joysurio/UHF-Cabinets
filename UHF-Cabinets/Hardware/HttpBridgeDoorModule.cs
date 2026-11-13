using System.Net.Http;
using System.Text.Json;
using UHFCabinets.Data.Configuration;

namespace UHFCabinets.Hardware;

/// <summary>
/// Production path: talks to a small local HTTP service that drives real door hardware.
/// Expected routes (relative to bridge base URL): POST <c>hardware/door/open</c>, GET <c>hardware/door/status</c> → <c>{"closed":true|false}</c>.
/// </summary>
public sealed class HttpBridgeDoorModule : IDoorModule
{
    private readonly HttpClient _http;
    private readonly HardwareSettings _hw;

    public HttpBridgeDoorModule(HttpClient http, HardwareSettings hw)
    {
        _http = http;
        _hw = hw;
    }

    public async Task RequestOpenDoorAsync(CancellationToken cancellationToken = default)
    {
        using var resp = await _http.PostAsync("hardware/door/open", null, cancellationToken).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException(
                $"Hardware bridge door open failed ({(int)resp.StatusCode}): {body.Trim()}".Trim());
        }
    }

    public async Task WaitForDoorClosedAsync(CancellationToken cancellationToken = default)
    {
        var poll = TimeSpan.FromMilliseconds(Math.Clamp(_hw.DoorClosedPollMilliseconds, 50, 5000));
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var resp = await _http.GetAsync("hardware/door/status", cancellationToken).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                throw new InvalidOperationException(
                    $"Hardware bridge door status failed ({(int)resp.StatusCode}): {body.Trim()}".Trim());
            }

            await using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (doc.RootElement.TryGetProperty("closed", out var c) &&
                c.ValueKind is JsonValueKind.True or JsonValueKind.False &&
                c.GetBoolean())
                return;

            await Task.Delay(poll, cancellationToken).ConfigureAwait(false);
        }
    }
}
