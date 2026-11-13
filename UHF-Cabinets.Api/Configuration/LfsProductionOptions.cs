namespace UHFCabinets.Api.Configuration;

/// <summary>
/// Test integration with LFSProduction. Configure BaseUrl for your environment; keep the API contract client-agnostic.
/// </summary>
public class LfsProductionOptions
{
    public const string SectionName = "LfsProduction";

    public string BaseUrl { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public bool Enabled { get; set; }
    public string MovementsPushPath { get; set; } = "/api/inbound/movements";
    public string RfidItemsPushPath { get; set; } = "/api/inbound/rfid-items";
    public int PollSeconds { get; set; } = 15;
}
