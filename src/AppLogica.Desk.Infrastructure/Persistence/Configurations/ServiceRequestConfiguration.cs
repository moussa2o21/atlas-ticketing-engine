using AppLogica.Desk.Domain.ServiceCatalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppLogica.Desk.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="ServiceRequest"/> aggregate root.
/// Maps to the "service_requests" table.
/// </summary>
public sealed class ServiceRequestConfiguration : IEntityTypeConfiguration<ServiceRequest>
{
    public void Configure(EntityTypeBuilder<ServiceRequest> builder)
    {
        builder.ToTable("service_requests");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.RequestNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(r => r.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(r => r.Description)
            .HasMaxLength(4000);

        builder.Property(r => r.CatalogItemId)
            .IsRequired();

        builder.Property(r => r.RequesterId)
            .IsRequired();

        builder.Property(r => r.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(r => r.ApprovalStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(r => r.FulfillmentNotes)
            .HasMaxLength(4000);

        builder.Property(r => r.CancellationReason)
            .HasMaxLength(4000);

        // FK to ServiceCatalogItem
        builder.HasOne<ServiceCatalogItem>()
            .WithMany()
            .HasForeignKey(r => r.CatalogItemId)
            .OnDelete(DeleteBehavior.Restrict);

        // Tenant isolation
        builder.Property(r => r.TenantId)
            .IsRequired();

        // Audit columns
        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.IsDeleted).IsRequired().HasDefaultValue(false);

        // Indexes
        builder.HasIndex(r => new { r.TenantId, r.Status })
            .HasDatabaseName("ix_service_requests_tenant_status");

        builder.HasIndex(r => new { r.TenantId, r.RequestNumber })
            .IsUnique()
            .HasDatabaseName("ix_service_requests_tenant_request_number");

        builder.HasIndex(r => new { r.TenantId, r.RequesterId })
            .HasDatabaseName("ix_service_requests_tenant_requester");

        builder.HasIndex(r => new { r.TenantId, r.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("ix_service_requests_tenant_created_desc");
    }
}
