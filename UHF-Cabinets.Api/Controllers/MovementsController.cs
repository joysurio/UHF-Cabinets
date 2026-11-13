using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UHFCabinets.Api.Contracts;
using UHFCabinets.Api.Data;
using UHFCabinets.Api.Data.Entities;
using UHFCabinets.Api.Validation;

namespace UHFCabinets.Api.Controllers;

[ApiController]
[Route("api/movements")]
public class MovementsController : ControllerBase
{
    private readonly ApiDbContext _db;
    private readonly ILogger<MovementsController> _logger;

    public MovementsController(ApiDbContext db, ILogger<MovementsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Register([FromBody] MovementRequest? body, CancellationToken cancellationToken)
    {
        var authCabinetId = HttpContext.Items["CabinetId"] as string;
        if (string.IsNullOrEmpty(authCabinetId))
            return Unauthorized(new { success = false, message = "Cabinet context missing." });

        var err = ValidateMovement(body, authCabinetId);
        if (err != null)
        {
            _logger.LogWarning("Movement rejected: {Reason}", err);
            return BadRequest(new { success = false, message = err });
        }

        try
        {
            var traceId = HttpContext.TraceIdentifier;
            _db.Movements.Add(new MovementRecord
            {
                IdMov = body!.IdMov.Trim(),
                Rfid = body.Rfid.Trim(),
                Usuario = body.Usuario.Trim(),
                TipoOperacion = MovementValidator.NormalizeTipo(body.TipoOperacion),
                Fecha = body.Fecha!.Value,
                CabinetId = body.CabinetId.Trim(),
                SyncStatus = SyncStatus.Pending,
                TraceId = traceId
            });
            await _db.SaveChangesAsync(cancellationToken);
            return Ok(new { success = true });
        }
        catch (DbUpdateException ex)
        {
            if (IsUniqueViolation(ex))
            {
                _logger.LogWarning(ex, "Duplicate movement");
                return Conflict(new { success = false, message = "Movement already registered for this idMov, rfid and cabinet." });
            }

            _logger.LogError(ex, "Failed to store movement");
            return StatusCode(500, new { success = false, message = "Could not store movement." });
        }
    }

    [HttpPost("bulk")]
    public async Task<IActionResult> RegisterBulk([FromBody] BulkMovementsRequest? body, CancellationToken cancellationToken)
    {
        var authCabinetId = HttpContext.Items["CabinetId"] as string;
        if (string.IsNullOrEmpty(authCabinetId))
            return Unauthorized(new { success = false, message = "Cabinet context missing." });

        if (body?.Items == null || body.Items.Count == 0)
            return BadRequest(new { success = false, message = "items is required." });

        var errors = new List<string>();
        for (var i = 0; i < body.Items.Count; i++)
        {
            var e = ValidateMovement(body.Items[i], authCabinetId);
            if (e != null)
                errors.Add($"items[{i}]: {e}");
        }

        if (errors.Count > 0)
        {
            _logger.LogWarning("Bulk movement rejected: {Errors}", string.Join("; ", errors));
            return BadRequest(new { success = false, message = string.Join(" ", errors) });
        }

        var traceId = HttpContext.TraceIdentifier;
        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var m in body.Items)
            {
                _db.Movements.Add(new MovementRecord
                {
                    IdMov = m.IdMov.Trim(),
                    Rfid = m.Rfid.Trim(),
                    Usuario = m.Usuario.Trim(),
                    TipoOperacion = MovementValidator.NormalizeTipo(m.TipoOperacion),
                    Fecha = m.Fecha!.Value,
                    CabinetId = m.CabinetId.Trim(),
                    SyncStatus = SyncStatus.Pending,
                    TraceId = traceId
                });
            }

            await _db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return Ok(new { success = true });
        }
        catch (DbUpdateException ex)
        {
            await tx.RollbackAsync(cancellationToken);
            if (IsUniqueViolation(ex))
            {
                _logger.LogWarning(ex, "Duplicate movement in bulk");
                return Conflict(new { success = false, message = "One or more movements were already registered." });
            }

            _logger.LogError(ex, "Failed to store bulk movements");
            return StatusCode(500, new { success = false, message = "Could not store movements." });
        }
    }

    private static string? ValidateMovement(MovementRequest? m, string authCabinetId)
    {
        if (m == null) return "Payload is required.";
        var baseErr = MovementValidator.Validate(m);
        if (baseErr != null) return baseErr;
        if (!string.Equals(m.CabinetId.Trim(), authCabinetId, StringComparison.Ordinal))
            return "cabinetId must match the authenticated cabinet.";
        return null;
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException != null &&
        (ex.InnerException.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) ||
         ex.InnerException.Message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase));
}
