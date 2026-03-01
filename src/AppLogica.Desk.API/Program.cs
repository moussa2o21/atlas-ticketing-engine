using System.Text.Json.Serialization;
using AppLogica.Desk.API.Hubs;
using AppLogica.Desk.API.Middleware;
using AppLogica.Desk.Application;
using AppLogica.Desk.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

// ──────────────────────────────────────────────────────────────────────────────
// ATLAS Desk — Ticketing Engine API
// ──────────────────────────────────────────────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);

// Default port for container deployments (overridable via ASPNETCORE_URLS)
builder.WebHost.ConfigureKestrel(options =>
{
    if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
    {
        options.ListenAnyIP(8080);
    }
});

// ─── Application & Infrastructure ────────────────────────────────────────────
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ─── Authentication — JwtBearer pointed at ATLAS Identity ────────────────────
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = builder.Configuration["Auth:Authority"];
        options.Audience = builder.Configuration["Auth:Audience"];
        options.RequireHttpsMetadata = false; // internal cluster communication
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false, // audience validation optional for now
            ValidateLifetime = true,
        };
    });

builder.Services.AddAuthorization();

// ─── Controllers + JSON ──────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// ─── Swagger / OpenAPI ───────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "ATLAS Desk API",
        Version = "v1",
        Description = "ATLAS Ticketing Engine — ITIL-aligned incident management API."
    });
});

// ─── SignalR ─────────────────────────────────────────────────────────────────
builder.Services.AddSignalR();

// ─── Health Checks ───────────────────────────────────────────────────────────
builder.Services.AddHealthChecks();

// ─── MassTransit (RabbitMQ) ──────────────────────────────────────────────────
// Configured in Infrastructure DependencyInjection if EVENTBUS__CONNECTIONSTRING is set.

// ──────────────────────────────────────────────────────────────────────────────
// Build the application
// ──────────────────────────────────────────────────────────────────────────────

var app = builder.Build();

// ─── Auto-Migrate (when APPLY_MIGRATIONS=true or --migrate flag) ────────
if (args.Contains("--migrate") || Environment.GetEnvironmentVariable("APPLY_MIGRATIONS") == "true")
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppLogica.Desk.Infrastructure.Persistence.DeskDbContext>();
    db.Database.Migrate();
    if (args.Contains("--migrate"))
    {
        // Exit after migration when running as a job
        return;
    }
}

// ─── Middleware Pipeline ─────────────────────────────────────────────────────
// TenantResolutionMiddleware must run before auth so that tenant context
// is available to downstream handlers. It only applies to /api/ routes.
app.UseMiddleware<TenantResolutionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

// ─── Swagger (Development only) ──────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ATLAS Desk API v1");
    });
}

// ─── Endpoint Mapping ────────────────────────────────────────────────────────
app.MapControllers();
app.MapHub<DeskHub>("/hubs/desk");

app.Run();

// ──────────────────────────────────────────────────────────────────────────────
// Partial class declaration for WebApplicationFactory in integration tests.
// ──────────────────────────────────────────────────────────────────────────────
public partial class Program { }
