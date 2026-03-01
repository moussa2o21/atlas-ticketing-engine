using AppLogica.Desk.Domain.Sla;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppLogica.Desk.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="SlaTimer"/> entity.
/// Maps to the "sla_timers" table with indexes optimized for the Hangfire SLA evaluation job.
/// </summary>
public sealed class SlaTimerConfiguration : IEntityTypeConfiguration<SlaTimer>
{
    public void Configure(EntityTypeBuilder<SlaTimer> builder)
    {
        builder.ToTable("sla_timers");

        // Primary key
        builder.HasKey(t => t.Id);

        // Properties
        builder.Property(t => t.IncidentId)
            .IsRequired();

        builder.Property(t => t.ResponseDueAt)
            .IsRequired();

        builder.Property(t => t.ResolutionDueAt)
            .IsRequired();

        builder.Property(t => t.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(t => t.PauseReason)
            .HasMaxLength(500);

        builder.Property(t => t.TenantId)
            .IsRequired();

        // Audit columns
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.IsDeleted).IsRequired().HasDefaultValue(false);

        // Composite index for the Hangfire SLA timer evaluation job
        builder.HasIndex(t => new { t.TenantId, t.Status, t.ResolutionDueAt })
            .HasDatabaseName("ix_sla_timers_tenant_status_resolution_due");

        // Index for looking up timers by incident
        builder.HasIndex(t => new { t.TenantId, t.IncidentId })
            .HasDatabaseName("ix_sla_timers_tenant_incident");
    }
}
