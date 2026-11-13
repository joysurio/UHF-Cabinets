using Microsoft.EntityFrameworkCore;
using UHFCabinets.Data.Models;
using UHFCabinets.Data.Services;

namespace UHFCabinets.Data;

public class AppDbContext : DbContext
{
    private readonly string _dbPath;

    public AppDbContext(string? dbPath = null)
    {
        _dbPath = dbPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "UHF-Cabinets", "uhfcabinets.db");
        var dir = Path.GetDirectoryName(_dbPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
    }

    public DbSet<CabinetItem> Items => Set<CabinetItem>();
    public DbSet<MovimientoEntity> Movimientos => Set<MovimientoEntity>();
    public DbSet<AppMetaEntry> AppMeta => Set<AppMetaEntry>();
    public DbSet<PendingApiSyncEntry> PendingApiSync => Set<PendingApiSyncEntry>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite($"Data Source={_dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MovimientoEntity>(e =>
        {
            e.HasIndex(x => new { x.IdMov, x.Rfid });
        });
    }

    public void EnsureCreatedAndSeed()
    {
        Database.EnsureCreated();
        if (!Items.Any())
        {
            Items.Add(new CabinetItem { Rfid = "E2000017221101441890A1B2", Producto = "Paracetamol 1g" });
            Items.Add(new CabinetItem { Rfid = "E2000017221101441890A1B3", Producto = "Paracetamol 1g" });
        }

        if (!AppMeta.Any(x => x.Key == LocalCabinetService.LastScanKey))
        {
            AppMeta.Add(new AppMetaEntry
            {
                Key = LocalCabinetService.LastScanKey,
                Value = "[\"E2000017221101441890A1B2\",\"E2000017221101441890A1B3\"]"
            });
        }

        SaveChanges();
    }
}
