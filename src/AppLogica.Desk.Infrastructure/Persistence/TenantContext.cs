using Microsoft.AspNetCore.Http;

namespace AppLogica.Desk.Infrastructure.Persistence;

/// <summary>
/// Resolves the current tenant from the JWT claims in the HTTP context.
/// Looks for the "tenant_id" claim first, then falls back to the "X-Tenant-Id" header.
/// Throws <see cref="UnauthorizedAccessException"/> if no tenant can be resolved.
/// </summary>
public sealed class TenantContext : ITenantContext
{
    private const string TenantClaimType = "tenant_id";
    private const string TenantHeaderName = "X-Tenant-Id";
    private const string DefaultSchemaName = "desk";

    private readonly Lazy<Guid> _tenantId;

    public TenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _tenantId = new Lazy<Guid>(() => ResolveTenantId(httpContextAccessor));
    }

    /// <inheritdoc />
    public string SchemaName => DefaultSchemaName;

    /// <inheritdoc />
    public Guid TenantId => _tenantId.Value;

    private static Guid ResolveTenantId(IHttpContextAccessor httpContextAccessor)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            throw new UnauthorizedAccessException(
                "No HTTP context available. Tenant cannot be resolved outside of an HTTP request.");
        }

        // 1. Try to resolve from JWT claim "tenant_id"
        var tenantClaim = httpContext.User.FindFirst(TenantClaimType);
        if (tenantClaim is not null && Guid.TryParse(tenantClaim.Value, out var tenantIdFromClaim))
        {
            return tenantIdFromClaim;
        }

        // 2. Fallback: try "X-Tenant-Id" header
        if (httpContext.Request.Headers.TryGetValue(TenantHeaderName, out var headerValue)
            && Guid.TryParse(headerValue.ToString(), out var tenantIdFromHeader))
        {
            return tenantIdFromHeader;
        }

        throw new UnauthorizedAccessException(
            $"Tenant identifier not found. Provide a '{TenantClaimType}' JWT claim or '{TenantHeaderName}' header.");
    }
}
