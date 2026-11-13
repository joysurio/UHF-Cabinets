using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using UHFCabinets.Api.Configuration;
using UHFCabinets.Api.Data.Entities;

namespace UHFCabinets.Api.Services;

public class LfsOutboundSyncService : ILfsOutboundSyncService
{
    private readonly HttpClient _http;
    private readonly LfsProductionOptions _options;
    private readonly ILogger<LfsOutboundSyncService> _logger;

    public LfsOutboundSyncService(
        HttpClient http,
        IOptions<LfsProductionOptions> options,
        ILogger<LfsOutboundSyncService> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<(bool Ok, string? Error)> TryPushMovementAsync(MovementRecord movement, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.BaseUrl))
            return (true, null);

        try
        {
            var uri = CombineUri(_options.BaseUrl, _options.MovementsPushPath);
            using var req = new HttpRequestMessage(HttpMethod.Post, uri);
            AddAuth(req);
            var payload = new
            {
                movement.IdMov,
                movement.Rfid,
                movement.Usuario,
                movement.TipoOperacion,
                Fecha = movement.Fecha,
                movement.CabinetId,
                movement.TraceId
            };
            req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var resp = await _http.SendAsync(req, cancellationToken);
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(cancellationToken);
                var err = $"LFS HTTP {(int)resp.StatusCode}: {body}";
                _logger.LogWarning("Movement sync failed for {IdMov} / {Rfid}: {Error}", movement.IdMov, movement.Rfid, err);
                return (false, err);
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Movement sync exception for {IdMov}", movement.IdMov);
            return (false, ex.Message);
        }
    }

    public async Task<(bool Ok, string? Error)> TryPushRfidItemAsync(RfidItemRecord item, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.BaseUrl))
            return (true, null);

        try
        {
            var uri = CombineUri(_options.BaseUrl, _options.RfidItemsPushPath);
            using var req = new HttpRequestMessage(HttpMethod.Post, uri);
            AddAuth(req);
            var payload = new { item.Rfid, item.IdProducto };
            req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var resp = await _http.SendAsync(req, cancellationToken);
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(cancellationToken);
                var err = $"LFS HTTP {(int)resp.StatusCode}: {body}";
                _logger.LogWarning("RFID item sync failed for {Rfid}: {Error}", item.Rfid, err);
                return (false, err);
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RFID item sync exception for {Rfid}", item.Rfid);
            return (false, ex.Message);
        }
    }

    private void AddAuth(HttpRequestMessage req)
    {
        if (!string.IsNullOrEmpty(_options.ApiKey))
            req.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_options.ApiKey}");
    }

    private static Uri CombineUri(string baseUrl, string path)
    {
        var b = baseUrl.TrimEnd('/');
        var p = path.StartsWith('/') ? path : "/" + path;
        return new Uri(b + p);
    }
}
