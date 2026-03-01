using AppLogica.Desk.Domain.Incidents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppLogica.Desk.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="Incident"/> entity.
/// Maps to the "incidents" table with appropriate column constraints and indexes.
/// </summary>
public sealed class IncidentConfiguration : IEntityTypeConfiguration<Incident>
{
    public void Configure(EntityTypeBuilder<Incident> builder)
    {
        builder.ToTable("incidents");

        // Primary key
        builder.HasKey(i => i.Id);

        // Properties
        builder.Property(i => i.TicketNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(i => i.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(i => i.Description)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(i => i.ResolutionNotes)
            .HasMaxLength(4000);

        // Enums stored as strings
        builder.Property(i => i.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(i => i.Priority)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(i => i.Impact)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(i => i.Urgency)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(i => i.IncidentType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        // Tenant isolation
        builder.Property(i => i.TenantId)
            .IsRequired();

        // Audit columns
        builder.Property(i => i.CreatedAt).IsRequired();
        builder.Property(i => i.IsDeleted).IsRequired().HasDefaultValue(false);

        // Indexes for common query patterns
        builder.HasIndex(i => new { i.TenantId, i.Status })
            .HasDatabaseName("ix_incidents_tenant_status");

        builder.HasIndex(i => new { i.TenantId, i.AssigneeId })
            .HasDatabaseName("ix_incidents_tenant_assignee");

        builder.HasIndex(i => new { i.TenantId, i.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("ix_incidents_tenant_created_desc");

        builder.HasIndex(i => i.TicketNumber)
            .IsUnique()
            .HasDatabaseName("ix_incidents_ticket_number");
    }
}
