using Microsoft.EntityFrameworkCore;
using UHFCabinets.Api.Data.Entities;

namespace UHFCabinets.Api.Data;

public class ApiDbContext : DbContext
{
    public ApiDbContext(DbContextOptions<ApiDbContext> options) : base(options) { }

    public DbSet<CabinetRegistration> CabinetRegistrations => Set<CabinetRegistration>();
    public DbSet<GatewayUser> GatewayUsers => Set<GatewayUser>();
    public DbSet<ProductRecord> Products => Set<ProductRecord>();
    public DbSet<RfidItemRecord> RfidItems => Set<RfidItemRecord>();
    public DbSet<MovementRecord> Movements => Set<MovementRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CabinetRegistration>(e =>
        {
            e.HasIndex(x => x.CabinetId).IsUnique();
        });

        modelBuilder.Entity<GatewayUser>(e =>
        {
            e.HasKey(x => x.IdUsuario);
            e.HasIndex(x => x.UserName).IsUnique();
        });

        modelBuilder.Entity<ProductRecord>(e =>
        {
            e.HasKey(x => x.IdProducto);
        });

        modelBuilder.Entity<RfidItemRecord>(e =>
        {
            e.HasKey(x => x.Rfid);
            e.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.IdProducto)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MovementRecord>(e =>
        {
            e.HasIndex(x => new { x.IdMov, x.Rfid, x.CabinetId }).IsUnique();
        });
    }
}
