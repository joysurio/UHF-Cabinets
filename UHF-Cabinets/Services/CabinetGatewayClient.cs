using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using UHFCabinets.Data.Configuration;
using UHFCabinets.Data.Services;

namespace UHFCabinets.Services;

public class CabinetGatewayClient
{
    private readonly HttpClient _http;
    private readonly AppSettings _settings;
    private readonly LocalCabinetService _local;

    public CabinetGatewayClient(HttpClient http, AppSettings settings, LocalCabinetService local)
    {
        _http = http;
        _settings = settings;
        _local = local;
    }

    public void ApplyDefaultHeaders()
    {
        _http.DefaultRequestHeaders.Remove("X-Cabinet-Id");
        _http.DefaultRequestHeaders.Remove("X-Api-Key");
        _http.DefaultRequestHeaders.TryAddWithoutValidation("X-Cabinet-Id", _settings.CabinetId);
        _http.DefaultRequestHeaders.TryAddWithoutValidation("X-Api-Key", _settings.CabinetApiKey);
    }

    public async Task<(bool Ok, string? UserId, string? Name, string? Role, string? Error)> LoginAsync(
        string username, string password, CancellationToken ct = default)
    {
        ApplyDefaultHeaders();
        var url = $"{_settings.ApiBaseUrl.TrimEnd('/')}/api/auth/login";
        var body = new { username, password };
        var resp = await _http.PostAsJsonAsync(url, body, ct);
        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var root = doc.RootElement;
        if (!resp.IsSuccessStatusCode || !root.TryGetProperty("success", out var s) || s.GetBoolean() != true)
        {
            var msg = root.TryGetProperty("message", out var m) ? m.GetString() : resp.ReasonPhrase;
            return (false, null, null, null, msg ?? "Login failed.");
        }

        if (!root.TryGetProperty("user", out var u))
            return (false, null, null, null, "Invalid login response.");
        var id = u.TryGetProperty("idUsuario", out var idEl) ? idEl.GetString() : null;
        var name = u.TryGetProperty("nombre", out var nEl) ? nEl.GetString() : null;
        var role = u.TryGetProperty("rol", out var rEl) && rEl.ValueKind != JsonValueKind.Null ? rEl.GetString() : null;
        if (string.IsNullOrEmpty(id))
            return (false, null, null, null, "Invalid user payload.");
        return (true, id, name ?? id, role, null);
    }

    public async Task RefreshProductsCatalogCacheAsync(CancellationToken ct = default)
    {
        ApplyDefaultHeaders();
        var url = $"{_settings.ApiBaseUrl.TrimEnd('/')}/api/products";
        var resp = await _http.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode) return;
        var json = await resp.Content.ReadAsStringAsync(ct);
        _local.SetProductsCatalogJson(json);
    }

    public async Task<bool> TryRegisterRfidAsync(string rfid, string idProducto, CancellationToken ct = default)
    {
        ApplyDefaultHeaders();
        var url = $"{_settings.ApiBaseUrl.TrimEnd('/')}/api/items";
        var resp = await _http.PostAsJsonAsync(url, new { rfid, idProducto }, ct);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> TryPostMovementAsync(
        string idMov, string rfid, string usuario, string tipoOperacion, DateTimeOffset fecha, CancellationToken ct = default)
    {
        ApplyDefaultHeaders();
        var url = $"{_settings.ApiBaseUrl.TrimEnd('/')}/api/movements";
        var body = new
        {
            idMov,
            rfid,
            usuario,
            tipoOperacion,
            fecha,
            cabinetId = _settings.CabinetId
        };
        var resp = await _http.PostAsJsonAsync(url, body, ct);
        return resp.IsSuccessStatusCode;
    }
}
