namespace UHFCabinets.Data.Configuration;

public class AppSettings
{
    public bool TestMode { get; set; } = true;

    public string ApiBaseUrl { get; set; } = "http://localhost:5288";

    public string CabinetId { get; set; } = "CAB-01";

    public string CabinetApiKey { get; set; } = "dev-cabinet-api-key";

    public int LoginTimeoutSeconds { get; set; } = 120;

    public int SyncIntervalSeconds { get; set; } = 30;

    /// <summary>Dev-only: accept any TLS server certificate (gateway + hardware bridge). Never enable in production.</summary>
    public bool AllowInsecureTls { get; set; }

    public HardwareSettings Hardware { get; set; } = new();

    public DeploymentSeedOptions? DeploymentSeed { get; set; }
}

public class HardwareSettings
{
    /// <summary>Default: stub | httpBridge | lectorUhf (native stack via ClsLectorUHF). Used when doorMode/rfidMode are omitted.</summary>
    public string Mode { get; set; } = "stub";

    /// <summary>Overrides door channel only (stub | httpBridge). Null = derived from <see cref="Mode"/> (lectorUhf on cabinet maps to stub door).</summary>
    public string? DoorMode { get; set; }

    /// <summary>Overrides RFID channel only (stub | httpBridge | lectorUhf). Null = <see cref="Mode"/>.</summary>
    public string? RfidMode { get; set; }

    /// <summary>Base URL of the cabinet hardware bridge (e.g. https://127.0.0.1:17700). Required when door uses httpBridge.</summary>
    public string? BridgeBaseUrl { get; set; }

    public int DoorClosedPollMilliseconds { get; set; } = 250;

    public string GetEffectiveDoorMode()
    {
        if (!string.IsNullOrWhiteSpace(DoorMode))
            return DoorMode.Trim();
        if (string.Equals(Mode, "lectorUhf", StringComparison.OrdinalIgnoreCase))
            return "stub";
        return Mode.Trim();
    }

    public string GetEffectiveRfidMode()
    {
        if (!string.IsNullOrWhiteSpace(RfidMode))
            return RfidMode.Trim();
        return Mode.Trim();
    }
}

public class DeploymentSeedOptions
{
    public bool ImportOnStartup { get; set; }

    /// <summary>Path to deployment JSON (absolute or relative to app base directory).</summary>
    public string? JsonPath { get; set; }
}
