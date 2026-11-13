using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UHFCabinets.Api.Contracts;
using UHFCabinets.Api.Data;
using UHFCabinets.Api.Data.Entities;

namespace UHFCabinets.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ApiDbContext _db;
    private readonly IPasswordHasher<GatewayUser> _passwordHasher;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        ApiDbContext db,
        IPasswordHasher<GatewayUser> passwordHasher,
        ILogger<AuthController> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest? body, CancellationToken cancellationToken)
    {
        if (body == null ||
            string.IsNullOrWhiteSpace(body.Username) ||
            string.IsNullOrWhiteSpace(body.Password))
        {
            _logger.LogWarning("Login rejected: missing credentials");
            return Unauthorized(new { success = false, message = "Invalid username or password" });
        }

        var user = await _db.GatewayUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserName == body.Username.Trim(), cancellationToken);

        if (user == null || !user.Activo)
        {
            _logger.LogWarning("Login failed for user {Username}: not found or inactive", body.Username);
            return Unauthorized(new { success = false, message = "Invalid username or password" });
        }

        var verify = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, body.Password);
        if (verify == PasswordVerificationResult.Failed)
        {
            _logger.LogWarning("Login failed for user {Username}: bad password", body.Username);
            return Unauthorized(new { success = false, message = "Invalid username or password" });
        }

        return Ok(new
        {
            success = true,
            user = new
            {
                idUsuario = user.IdUsuario,
                nombre = user.Nombre,
                activo = user.Activo,
                rol = user.Rol
            }
        });
    }
}
