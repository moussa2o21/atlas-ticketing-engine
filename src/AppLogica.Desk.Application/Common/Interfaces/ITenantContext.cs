namespace AppLogica.Desk.Application.Common.Interfaces;

/// <summary>
/// Provides tenant context resolved from the current HTTP request (JWT claims).
/// Implemented in the Infrastructure layer by <c>TenantContext</c>.
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
