using AppLogica.Desk.Domain.ServiceCatalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppLogica.Desk.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="FulfillmentTask"/> entity.
/// Maps to the "fulfillment_tasks" table.
/// </summary>
public sealed class FulfillmentTaskConfiguration : IEntityTypeConfiguration<FulfillmentTask>
{
    public void Configure(EntityTypeBuilder<FulfillmentTask> builder)
    {
        builder.ToTable("fulfillment_tasks");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.ServiceRequestId)
            .IsRequired();

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(t => t.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(t => t.Notes)
            .HasMaxLength(4000);

        // FK to ServiceRequest
        builder.HasOne<ServiceRequest>()
            .WithMany()
            .HasForeignKey(t => t.ServiceRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        // Tenant isolation
        builder.Property(t => t.TenantId)
            .IsRequired();

        // Audit columns
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.IsDeleted).IsRequired().HasDefaultValue(false);

        // Indexes
        builder.HasIndex(t => new { t.TenantId, t.ServiceRequestId })
            .HasDatabaseName("ix_fulfillment_tasks_tenant_request");

        builder.HasIndex(t => new { t.TenantId, t.Status })
            .HasDatabaseName("ix_fulfillment_tasks_tenant_status");
    }
}
