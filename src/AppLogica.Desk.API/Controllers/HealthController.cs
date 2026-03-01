using AppLogica.Desk.Infrastructure.Persistence;
using MassTransit;
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
        [FromServices] IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var postgresStatus = "unhealthy";
        var rabbitmqStatus = "not_configured";

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

        // Check RabbitMQ connectivity via MassTransit IBusControl
        var busControl = serviceProvider.GetService<IBusControl>();
        if (busControl is not null)
        {
            try
            {
                var healthResult = busControl.CheckHealth();
                rabbitmqStatus = healthResult.Status switch
                {
                    BusHealthStatus.Healthy => "healthy",
                    BusHealthStatus.Degraded => "degraded",
                    _ => "unhealthy"
                };
            }
            catch
            {
                rabbitmqStatus = "degraded";
            }
        }

        // Overall: healthy only if postgres is healthy AND rabbitmq is healthy or not_configured
        var overallStatus = postgresStatus == "healthy"
            && rabbitmqStatus is "healthy" or "not_configured"
                ? "healthy"
                : "degraded";

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
