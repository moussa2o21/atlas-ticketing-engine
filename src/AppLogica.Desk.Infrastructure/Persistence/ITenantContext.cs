namespace AppLogica.Desk.Infrastructure.Persistence;

/// <summary>
/// Provides tenant context resolved from the current HTTP request (JWT claims).
/// Used by <see cref="DeskDbContext"/> to enforce multi-tenant data isolation.
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// The database schema name for the current tenant.
    /// </summary>
    string SchemaName { get; }

    /// <summary>
    /// The tenant identifier resolved from the JWT claim or request header.
    /// </summary>
    Guid TenantId { get; }
}
