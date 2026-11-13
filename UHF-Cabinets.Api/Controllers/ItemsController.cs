using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UHFCabinets.Api.Contracts;
using UHFCabinets.Api.Data;
using UHFCabinets.Api.Data.Entities;

namespace UHFCabinets.Api.Controllers;

[ApiController]
[Route("api/items")]
public class ItemsController : ControllerBase
{
    private readonly ApiDbContext _db;
    private readonly ILogger<ItemsController> _logger;

    public ItemsController(ApiDbContext db, ILogger<ItemsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet("{rfid}")]
    public async Task<IActionResult> GetByRfid(string rfid, CancellationToken cancellationToken)
    {
        var row = await _db.RfidItems.AsNoTracking()
            .Include(x => x.Product)
            .FirstOrDefaultAsync(x => x.Rfid == rfid, cancellationToken);

        if (row == null)
            return Ok(new { exists = false });

        return Ok(new
        {
            exists = true,
            item = new
            {
                rfid = row.Rfid,
                idProducto = row.IdProducto,
                producto = row.Product != null ? row.Product.Nombre : ""
            }
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRfidItemRequest? body, CancellationToken cancellationToken)
    {
        if (body == null || string.IsNullOrWhiteSpace(body.Rfid) || string.IsNullOrWhiteSpace(body.IdProducto))
        {
            _logger.LogWarning("Create RFID rejected: missing fields");
            return BadRequest(new { success = false, message = "rfid and idProducto are required." });
        }

        var rfid = body.Rfid.Trim();
        var idProducto = body.IdProducto.Trim();

        if (await _db.RfidItems.AnyAsync(x => x.Rfid == rfid, cancellationToken))
            return Conflict(new { success = false, message = "RFID already exists." });

        if (!await _db.Products.AnyAsync(p => p.IdProducto == idProducto, cancellationToken))
            return BadRequest(new { success = false, message = "Product does not exist." });

        _db.RfidItems.Add(new RfidItemRecord
        {
            Rfid = rfid,
            IdProducto = idProducto,
            SyncStatus = SyncStatus.Pending
        });

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { success = true });
    }
}
