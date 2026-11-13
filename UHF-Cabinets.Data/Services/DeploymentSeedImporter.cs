using System.Text.Json;

namespace UHFCabinets.Data.Services;

/// <summary>Import <c>items</c> and optional <c>lastScanRfids</c> from a deployment JSON file.</summary>
public static class DeploymentSeedImporter
{
    public static void TryImport(LocalCabinetService local, string? jsonPath, bool importOnStartup)
    {
        if (!importOnStartup || string.IsNullOrWhiteSpace(jsonPath))
            return;

        var path = Path.IsPathRooted(jsonPath)
            ? jsonPath
            : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, jsonPath);

        if (!File.Exists(path))
            return;

        string json;
        try
        {
            json = File.ReadAllText(path);
        }
        catch
        {
            return;
        }

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
        {
            foreach (var el in items.EnumerateArray())
            {
                var rfid = el.TryGetProperty("rfid", out var r) ? r.GetString() : null;
                var prod = el.TryGetProperty("producto", out var p) ? p.GetString() : null;
                if (!string.IsNullOrWhiteSpace(rfid) && !string.IsNullOrWhiteSpace(prod))
                    local.UpsertItem(rfid, prod);
            }
        }

        if (root.TryGetProperty("lastScanRfids", out var scan) && scan.ValueKind == JsonValueKind.Array)
        {
            var list = scan.EnumerateArray()
                .Select(e => e.ValueKind == JsonValueKind.String ? e.GetString() : null)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Cast<string>()
                .Distinct(StringComparer.Ordinal)
                .ToList();
            if (list.Count > 0)
                local.SetLastScanRfids(list);
        }
    }
}
