using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UHFCabinets.Api.Data;

namespace UHFCabinets.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly ApiDbContext _db;

    public ProductsController(ApiDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var q = _db.Products.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            q = q.Where(p =>
                EF.Functions.Like(p.Codigo, $"%{term}%") ||
                EF.Functions.Like(p.Nombre, $"%{term}%") ||
                EF.Functions.Like(p.IdProducto, $"%{term}%"));
        }

        var items = await q
            .OrderBy(p => p.Nombre)
            .Select(p => new
            {
                idProducto = p.IdProducto,
                codigo = p.Codigo,
                nombre = p.Nombre
            })
            .ToListAsync(cancellationToken);

        return Ok(new { items });
    }

    [HttpGet("{idProducto}")]
    public async Task<IActionResult> GetById(string idProducto, CancellationToken cancellationToken)
    {
        var p = await _db.Products.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdProducto == idProducto, cancellationToken);
        if (p == null)
            return NotFound(new { success = false, message = "Product not found." });

        return Ok(new
        {
            idProducto = p.IdProducto,
            codigo = p.Codigo,
            nombre = p.Nombre
        });
    }
}
