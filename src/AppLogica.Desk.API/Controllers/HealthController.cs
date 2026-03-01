using AppLogica.Desk.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppLogica.Desk.API.Controllers;

/// <summary>
/// Health check endpoints for Kubernetes liveness and readiness probes.
/// </summary>
[ApiController]
[AllowAnonymous]
public sealed class HealthController : ControllerBase
{
    /// <summary>
    /// Liveness probe — always returns 200 if the process is running.
    /// Kubernetes uses this to decide if the pod should be restarted.
    /// </summary>
    [HttpGet("/health/live")]
    public IActionResult Live()
    {
        return Ok(new { status = "alive" });
    }

    /// <summary>
    /// Readiness probe — checks downstream dependencies (Postgres, RabbitMQ).
    /// Kubernetes uses this to decide if the pod should receive traffic.
    /// Returns 200 if all dependencies are healthy, 503 if any are unhealthy.
    /// </summary>
    [HttpGet("/health/ready")]
    public async Task<IActionResult> Ready(
        [FromServices] DeskDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var postgresStatus = "unhealthy";
        var rabbitmqStatus = "degraded"; // Will be connected in deployment

        // Check PostgreSQL connectivity
        try
        {
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
            postgresStatus = canConnect ? "healthy" : "unhealthy";
        }
        catch
        {
            postgresStatus = "unhealthy";
        }

        // RabbitMQ: return "degraded" for now — full connectivity check will be
        // implemented when MassTransit consumers are deployed.
        // In production, this would check IBusControl.CheckHealth() or similar.

        var overallStatus = postgresStatus == "healthy" ? "healthy" : "degraded";

        var response = new
        {
            status = overallStatus,
            checks = new
            {
                postgres = postgresStatus,
                rabbitmq = rabbitmqStatus
            }
        };

        return overallStatus == "healthy"
            ? Ok(response)
            : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
    }
}
