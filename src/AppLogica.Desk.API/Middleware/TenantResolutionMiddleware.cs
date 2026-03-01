using Microsoft.AspNetCore.Http;

namespace AppLogica.Desk.API.Middleware;

/// <summary>
/// Middleware that extracts TenantId from JWT claims or request headers and stores it
/// in <see cref="HttpContext.Items"/> for downstream consumption.
/// Only applies to /api/ routes — health checks and SignalR hubs are skipped.
/// Returns 403 Forbidden if no tenant can be resolved on authenticated API routes.
/// </summary>
public sealed class TenantResolutionMiddleware
{
    private const string TenantClaimType = "tenant_id";
    private const string TenantHeaderName = "X-Tenant-Id";
    private const string TenantItemKey = "TenantId";

    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Only apply tenant resolution to /api/ routes
        if (!path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Try to resolve TenantId from JWT claim "tenant_id"
        var tenantClaim = context.User.FindFirst(TenantClaimType);
        if (tenantClaim is not null && Guid.TryParse(tenantClaim.Value, out var tenantIdFromClaim))
        {
            context.Items[TenantItemKey] = tenantIdFromClaim;
            await _next(context);
            return;
        }

        // Fallback: try "X-Tenant-Id" header
        if (context.Request.Headers.TryGetValue(TenantHeaderName, out var headerValue)
            && Guid.TryParse(headerValue.ToString(), out var tenantIdFromHeader))
        {
            context.Items[TenantItemKey] = tenantIdFromHeader;
            await _next(context);
            return;
        }

        // If the user is authenticated on an /api/ route but no tenant was resolved, reject
        if (context.User.Identity?.IsAuthenticated == true)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://tools.ietf.org/html/rfc9110#section-15.5.4",
                title = "Forbidden",
                status = 403,
                detail = $"Tenant identifier not found. Provide a '{TenantClaimType}' JWT claim or '{TenantHeaderName}' header."
            });
            return;
        }

        // Unauthenticated requests to /api/ routes will be caught by the [Authorize] attribute
        await _next(context);
    }
}
