using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using UHFCabinets.Data.Models;

namespace UHFCabinets.Data.Services;

public class LocalCabinetService
{
    public const string LastScanKey = "last_scan_rfids";
    public const string ProductsCatalogKey = "products_catalog_json";
    public const string SyncKindMovement = "Movement";
    public const string SyncKindRfid = "RfidRegister";

    private readonly AppDbContext _db;

    public LocalCabinetService(AppDbContext db) => _db = db;

    public IReadOnlyList<string> GetLastScanRfids()
    {
        var entry = _db.AppMeta.AsNoTracking().FirstOrDefault(x => x.Key == LastScanKey);
        if (entry == null || string.IsNullOrWhiteSpace(entry.Value))
            return Array.Empty<string>();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(entry.Value) ?? new List<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    public void SetLastScanRfids(IEnumerable<string> rfids)
    {
        var list = rfids.Distinct(StringComparer.Ordinal).ToList();
        var json = JsonSerializer.Serialize(list);
        var row = _db.AppMeta.FirstOrDefault(x => x.Key == LastScanKey);
        if (row == null)
            _db.AppMeta.Add(new AppMetaEntry { Key = LastScanKey, Value = json });
        else
            row.Value = json;
        _db.SaveChanges();
    }

    public bool HasItemRfid(string rfid) =>
        _db.Items.AsNoTracking().Any(x => x.Rfid == rfid);

    public void UpsertItem(string rfid, string producto)
    {
        var r = rfid.Trim();
        var p = producto.Trim();
        var row = _db.Items.FirstOrDefault(x => x.Rfid == r);
        if (row == null)
            _db.Items.Add(new CabinetItem { Rfid = r, Producto = p });
        else
            row.Producto = p;
        _db.SaveChanges();
    }

    public IReadOnlyList<CabinetItem> GetAllItems() =>
        _db.Items.AsNoTracking().OrderBy(x => x.Producto).ToList();

    public IReadOnlyList<(string Producto, int Quantity)> GetInventoryAggregatedByProduct()
    {
        var scan = GetLastScanRfids();
        if (scan.Count == 0)
            return Array.Empty<(string, int)>();

        var map = _db.Items.AsNoTracking().ToDictionary(x => x.Rfid, x => x.Producto, StringComparer.Ordinal);
        return scan
            .GroupBy(tag => map.TryGetValue(tag, out var prod) ? prod : "Unknown")
            .Select(g => (g.Key, g.Count()))
            .OrderBy(x => x.Key)
            .ToList();
    }

    public void AddMovimiento(string idMov, string rfid, string usuario, string tipoOperacion, DateTimeOffset fechaUtc)
    {
        _db.Movimientos.Add(new MovimientoEntity
        {
            IdMov = idMov,
            Rfid = rfid,
            Usuario = usuario,
            TipoOperacion = tipoOperacion.ToUpperInvariant(),
            Fecha = fechaUtc.ToString("O")
        });
        _db.SaveChanges();
    }

    public IReadOnlyList<PendingApiSyncEntry> GetPendingSync() =>
        _db.PendingApiSync.OrderBy(x => x.Id).ToList();

    public void EnqueueSync(string kind, object payload)
    {
        _db.PendingApiSync.Add(new PendingApiSyncEntry
        {
            Kind = kind,
            PayloadJson = JsonSerializer.Serialize(payload),
            CreatedUtc = DateTime.UtcNow
        });
        _db.SaveChanges();
    }

    public void RemovePending(int id)
    {
        var row = _db.PendingApiSync.FirstOrDefault(x => x.Id == id);
        if (row != null)
        {
            _db.PendingApiSync.Remove(row);
            _db.SaveChanges();
        }
    }

    public string? GetProductsCatalogJson() =>
        _db.AppMeta.AsNoTracking().FirstOrDefault(x => x.Key == ProductsCatalogKey)?.Value;

    public void SetProductsCatalogJson(string json)
    {
        var row = _db.AppMeta.FirstOrDefault(x => x.Key == ProductsCatalogKey);
        if (row == null)
            _db.AppMeta.Add(new AppMetaEntry { Key = ProductsCatalogKey, Value = json });
        else
            row.Value = json;
        _db.SaveChanges();
    }

    public IReadOnlyList<string> GetDistinctProductoFromItems() =>
        _db.Items.AsNoTracking().Select(x => x.Producto).Distinct().OrderBy(x => x).ToList();
}

