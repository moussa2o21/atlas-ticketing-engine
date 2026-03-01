using AppLogica.Desk.Domain.Common;
using AppLogica.Desk.Domain.Incidents;
using AppLogica.Desk.Domain.Sla;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AppLogica.Desk.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for the ATLAS Desk module. Applies tenant-scoped query filters,
/// soft-delete filters, audit timestamps, and domain event dispatch on save.
/// </summary>
public sealed class DeskDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;
    private readonly IPublisher _publisher;

    public DeskDbContext(
        DbContextOptions<DeskDbContext> options,
        ITenantContext tenantContext,
        IPublisher publisher)
        : base(options)
    {
        _tenantContext = tenantContext;
        _publisher = publisher;
    }

    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<SlaPolicy> SlaPolicies => Set<SlaPolicy>();
    public DbSet<SlaTimer> SlaTimers => Set<SlaTimer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Set the default schema from the tenant context
        modelBuilder.HasDefaultSchema(_tenantContext.SchemaName);

        // Apply all IEntityTypeConfiguration<T> from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DeskDbContext).Assembly);

        // Apply global query filters for soft-delete and tenant isolation
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // All entities that inherit from Entity have IsDeleted and TenantId
            if (typeof(Entity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .HasQueryFilter(CreateFilterExpression(entityType.ClrType));
            }
        }
    }

    /// <summary>
    /// Overrides SaveChangesAsync to set audit timestamps and dispatch domain events.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetAuditTimestamps();

        // Collect domain events before saving
        var domainEvents = GetDomainEvents();

        var result = await base.SaveChangesAsync(cancellationToken);

        // Dispatch domain events after successful persistence
        await DispatchDomainEventsAsync(domainEvents, cancellationToken);

        return result;
    }

    private void SetAuditTimestamps()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (entry.Entity.CreatedAt == default)
                    {
                        entry.Entity.CreatedAt = now;
                    }
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    break;
            }
        }
    }

    private List<IDomainEvent> GetDomainEvents()
    {
        var aggregateRoots = ChangeTracker.Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = aggregateRoots
            .SelectMany(ar => ar.DomainEvents)
            .ToList();

        foreach (var aggregateRoot in aggregateRoots)
        {
            aggregateRoot.ClearDomainEvents();
        }

        return domainEvents;
    }

    private async Task DispatchDomainEventsAsync(
        List<IDomainEvent> domainEvents,
        CancellationToken cancellationToken)
    {
        foreach (var domainEvent in domainEvents)
        {
            await _publisher.Publish(domainEvent, cancellationToken);
        }
    }

    /// <summary>
    /// Builds a lambda expression: (Entity e) => !e.IsDeleted && e.TenantId == _tenantContext.TenantId
    /// for the given entity type, applied as a global query filter.
    /// </summary>
    private System.Linq.Expressions.LambdaExpression CreateFilterExpression(Type entityType)
    {
        // Build: e => !e.IsDeleted && e.TenantId == _tenantContext.TenantId
        var parameter = System.Linq.Expressions.Expression.Parameter(entityType, "e");

        // !e.IsDeleted
        var isDeletedProperty = System.Linq.Expressions.Expression.Property(parameter, nameof(Entity.IsDeleted));
        var notDeleted = System.Linq.Expressions.Expression.Not(isDeletedProperty);

        // e.TenantId == _tenantContext.TenantId
        var tenantIdProperty = System.Linq.Expressions.Expression.Property(parameter, nameof(Entity.TenantId));

        // We must use a member access on _tenantContext so that the filter evaluates per-request
        var tenantContextConstant = System.Linq.Expressions.Expression.Constant(this);
        var tenantContextField = System.Linq.Expressions.Expression.Field(
            tenantContextConstant,
            typeof(DeskDbContext).GetField("_tenantContext",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!);
        var tenantContextTenantId = System.Linq.Expressions.Expression.Property(
            tenantContextField,
            nameof(ITenantContext.TenantId));

        var tenantIdEquals = System.Linq.Expressions.Expression.Equal(tenantIdProperty, tenantContextTenantId);

        // Combine: !e.IsDeleted && e.TenantId == _tenantContext.TenantId
        var combined = System.Linq.Expressions.Expression.AndAlso(notDeleted, tenantIdEquals);

        return System.Linq.Expressions.Expression.Lambda(combined, parameter);
    }
}
