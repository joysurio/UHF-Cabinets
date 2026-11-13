using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UHFCabinets.Api.Data.Entities;
using UHFCabinets.Api.Security;

namespace UHFCabinets.Api.Data;

public static class DatabaseSeeder
{
    public static async Task EnsureSeededAsync(ApiDbContext db, CancellationToken cancellationToken = default)
    {
        await db.Database.EnsureCreatedAsync(cancellationToken);

        if (!await db.CabinetRegistrations.AnyAsync(cancellationToken))
        {
            var demoKey = "dev-cabinet-api-key";
            db.CabinetRegistrations.Add(new CabinetRegistration
            {
                CabinetId = "CAB-01",
                ApiKeyHash = ApiKeyHasher.Hash(demoKey),
                IsActive = true
            });
        }

        if (!await db.GatewayUsers.AnyAsync(cancellationToken))
        {
            var hasher = new PasswordHasher<GatewayUser>();
            var user = new GatewayUser
            {
                IdUsuario = "U123",
                UserName = "admin",
                Nombre = "Admin User",
                Activo = true,
                Rol = "OPERADOR",
                SyncStatus = SyncStatus.Pending
            };
            user.PasswordHash = hasher.HashPassword(user, "Admin123!");
            db.GatewayUsers.Add(user);
        }

        if (!await db.Products.AnyAsync(cancellationToken))
        {
            db.Products.Add(new ProductRecord
            {
                IdProducto = "P001",
                Codigo = "PARA1G",
                Nombre = "Paracetamol 1g",
                SyncStatus = SyncStatus.Pending
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
