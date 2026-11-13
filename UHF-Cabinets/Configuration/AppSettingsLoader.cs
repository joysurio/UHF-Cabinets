using System.IO;
using System.Text.Json;
using UHFCabinets.Data.Configuration;

namespace UHFCabinets.Configuration;

public static class AppSettingsLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static AppSettings LoadFromAppDirectory()
    {
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        var path = Path.Combine(dir, "appsettings.json");

        AppSettings settings;
        if (!File.Exists(path))
            settings = new AppSettings();
        else
        {
            try
            {
                var json = File.ReadAllText(path);
                settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            }
            catch
            {
                settings = new AppSettings();
            }
        }

        var secretsPath = Path.Combine(dir, "appsettings.secrets.json");
        if (File.Exists(secretsPath))
            TryMergeSecrets(settings, secretsPath);

        ApplyEnvironment(settings);
        return settings;
    }

    private static void ApplyEnvironment(AppSettings s)
    {
        var api = Environment.GetEnvironmentVariable("UHF_API_BASE_URL");
        if (!string.IsNullOrWhiteSpace(api))
            s.ApiBaseUrl = api.Trim();

        var id = Environment.GetEnvironmentVariable("UHF_CABINET_ID");
        if (!string.IsNullOrWhiteSpace(id))
            s.CabinetId = id.Trim();

        var key = Environment.GetEnvironmentVariable("UHF_CABINET_API_KEY");
        if (!string.IsNullOrWhiteSpace(key))
            s.CabinetApiKey = key.Trim();

        var bridge = Environment.GetEnvironmentVariable("UHF_HARDWARE_BRIDGE_URL");
        if (!string.IsNullOrWhiteSpace(bridge))
        {
            s.Hardware.BridgeBaseUrl = bridge.Trim();
            s.Hardware.Mode = "httpBridge";
        }

        var insecure = Environment.GetEnvironmentVariable("UHF_ALLOW_INSECURE_TLS");
        if (string.Equals(insecure, "1", StringComparison.Ordinal) ||
            string.Equals(insecure, "true", StringComparison.OrdinalIgnoreCase))
            s.AllowInsecureTls = true;
    }

    private static void TryMergeSecrets(AppSettings s, string secretsPath)
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(secretsPath));
            var root = doc.RootElement;

            if (root.TryGetProperty("apiBaseUrl", out var api) && api.ValueKind == JsonValueKind.String)
                s.ApiBaseUrl = api.GetString() ?? s.ApiBaseUrl;
            if (root.TryGetProperty("cabinetId", out var cid) && cid.ValueKind == JsonValueKind.String)
                s.CabinetId = cid.GetString() ?? s.CabinetId;
            if (root.TryGetProperty("cabinetApiKey", out var ck) && ck.ValueKind == JsonValueKind.String)
                s.CabinetApiKey = ck.GetString() ?? s.CabinetApiKey;
            if (root.TryGetProperty("allowInsecureTls", out var tls) && tls.ValueKind is JsonValueKind.True or JsonValueKind.False)
                s.AllowInsecureTls = tls.GetBoolean();
            if (root.TryGetProperty("testMode", out var tm) && tm.ValueKind is JsonValueKind.True or JsonValueKind.False)
                s.TestMode = tm.GetBoolean();

            if (root.TryGetProperty("hardware", out var hw))
            {
                if (hw.TryGetProperty("mode", out var mode) && mode.ValueKind == JsonValueKind.String)
                    s.Hardware.Mode = mode.GetString() ?? s.Hardware.Mode;
                if (hw.TryGetProperty("doorMode", out var dm) && dm.ValueKind == JsonValueKind.String)
                    s.Hardware.DoorMode = dm.GetString();
                if (hw.TryGetProperty("rfidMode", out var rm) && rm.ValueKind == JsonValueKind.String)
                    s.Hardware.RfidMode = rm.GetString();
                if (hw.TryGetProperty("bridgeBaseUrl", out var bu) && bu.ValueKind == JsonValueKind.String)
                    s.Hardware.BridgeBaseUrl = bu.GetString();
                if (hw.TryGetProperty("doorClosedPollMilliseconds", out var poll) && poll.TryGetInt32(out var ms))
                    s.Hardware.DoorClosedPollMilliseconds = ms;
            }

            if (root.TryGetProperty("deploymentSeed", out var seed))
            {
                s.DeploymentSeed ??= new DeploymentSeedOptions();
                if (seed.TryGetProperty("importOnStartup", out var imp) && imp.ValueKind is JsonValueKind.True or JsonValueKind.False)
                    s.DeploymentSeed.ImportOnStartup = imp.GetBoolean();
                if (seed.TryGetProperty("jsonPath", out var jp) && jp.ValueKind == JsonValueKind.String)
                    s.DeploymentSeed.JsonPath = jp.GetString();
            }
        }
        catch
        {
            /* ignore malformed secrets */
        }
    }
}
