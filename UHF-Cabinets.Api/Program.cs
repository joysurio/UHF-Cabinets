using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using UHFCabinets.Api.Configuration;
using UHFCabinets.Api.Data;
using UHFCabinets.Api.Data.Entities;
using UHFCabinets.Api.Middleware;
using UHFCabinets.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<LfsProductionOptions>(builder.Configuration.GetSection(LfsProductionOptions.SectionName));

var dataDir = Path.Combine(builder.Environment.ContentRootPath, "Data");
Directory.CreateDirectory(dataDir);
var dbPath = Path.Combine(dataDir, "uhf_gateway.db");

builder.Services.AddDbContext<ApiDbContext>(o =>
    o.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddHttpClient<ILfsOutboundSyncService, LfsOutboundSyncService>();
builder.Services.AddSingleton<IPasswordHasher<GatewayUser>, PasswordHasher<GatewayUser>>();
builder.Services.AddHostedService<OutboundSyncProcessorHostedService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "UHF Cabinets Gateway", Version = "v1" });
    c.AddSecurityDefinition("X-Cabinet-Id", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = "X-Cabinet-Id",
        Description = "Cabinet id registered in the gateway (demo: CAB-01)."
    });
    c.AddSecurityDefinition("X-Api-Key", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = "X-Api-Key",
        Description = "Cabinet API key (demo seed: dev-cabinet-api-key)."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "X-Cabinet-Id" }
            },
            Array.Empty<string>()
        },
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "X-Api-Key" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApiDbContext>();
    await DatabaseSeeder.EnsureSeededAsync(db);
}

var enableSwagger = app.Environment.IsDevelopment()
    || app.Configuration.GetValue("EnableSwagger", false);

if (enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "UHF Cabinets Gateway v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseRouting();

app.Use(async (context, next) =>
{
    if (enableSwagger && context.Request.Path == "/")
    {
        context.Response.Redirect("/swagger");
        return;
    }
    await next(context);
});

app.UseMiddleware<CabinetAuthenticationMiddleware>();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
