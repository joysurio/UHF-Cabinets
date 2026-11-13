using Microsoft.EntityFrameworkCore;
using UHFCabinets.Api.Data;
using UHFCabinets.Api.Security;

namespace UHFCabinets.Api.Middleware;

public class CabinetAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CabinetAuthenticationMiddleware> _logger;

    public CabinetAuthenticationMiddleware(RequestDelegate next, ILogger<CabinetAuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IServiceScopeFactory scopeFactory)
    {
        if (ShouldBypass(context.Request.Path))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("X-Cabinet-Id", out var cabIdValues) ||
            !context.Request.Headers.TryGetValue("X-Api-Key", out var apiKeyValues))
        {
            _logger.LogWarning("Rejected request without cabinet headers: {Path}", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { success = false, message = "Missing cabinet authentication headers." });
            return;
        }

        var cabId = cabIdValues.ToString();
        var apiKey = apiKeyValues.ToString();

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApiDbContext>();
        var registration = await db.CabinetRegistrations
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CabinetId == cabId && x.IsActive);

        if (registration == null || !ApiKeyHasher.Verify(apiKey, registration.ApiKeyHash))
        {
            _logger.LogWarning("Invalid cabinet credentials for CabinetId={CabinetId}", cabId);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { success = false, message = "Invalid cabinet credentials." });
            return;
        }

        context.Items["CabinetId"] = registration.CabinetId;
        context.Response.Headers.Append("X-Trace-Id", context.TraceIdentifier);
        await _next(context);
    }

    private static bool ShouldBypass(PathString path)
    {
        if (path.StartsWithSegments("/health")) return true;
        if (path.StartsWithSegments("/swagger")) return true;
        return false;
    }
}
