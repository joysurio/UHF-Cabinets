using System.Net.Http;
using System.Text.Json;

namespace UHFCabinets.Hardware;

/// <summary>
/// GET <c>hardware/rfid/tags</c> → <c>{"tags":["epc",...]}</c> (strings; duplicates optional — bridge may normalize).
/// </summary>
public sealed class HttpBridgeRfidModule : IRfidModule
{
    private readonly HttpClient _http;

    public HttpBridgeRfidModule(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<string>> ReadTagsAsync(CancellationToken cancellationToken = default)
    {
        using var resp = await _http.GetAsync("hardware/rfid/tags", cancellationToken).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException(
                $"Hardware bridge RFID read failed ({(int)resp.StatusCode}): {body.Trim()}".Trim());
        }

        await using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (!doc.RootElement.TryGetProperty("tags", out var arr) || arr.ValueKind != JsonValueKind.Array)
            return Array.Empty<string>();

        var list = new List<string>();
        foreach (var el in arr.EnumerateArray())
        {
            if (el.ValueKind != JsonValueKind.String)
                continue;
            var s = el.GetString();
            if (!string.IsNullOrWhiteSpace(s))
                list.Add(s.Trim());
        }

        return list.Distinct(StringComparer.Ordinal).ToList();
    }
}
