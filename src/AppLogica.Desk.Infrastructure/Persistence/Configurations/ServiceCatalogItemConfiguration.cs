using AppLogica.Desk.Domain.ServiceCatalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppLogica.Desk.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="ServiceCatalogItem"/> entity.
/// Maps to the "service_catalog_items" table.
/// </summary>
public sealed class ServiceCatalogItemConfiguration : IEntityTypeConfiguration<ServiceCatalogItem>
{
    public void Configure(EntityTypeBuilder<ServiceCatalogItem> builder)
    {
        builder.ToTable("service_catalog_items");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Name)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(i => i.Description)
            .HasMaxLength(4000);

        builder.Property(i => i.FulfillmentInstructions)
            .HasMaxLength(8000);

        builder.Property(i => i.ExpectedDeliveryMinutes)
            .IsRequired();

        builder.Property(i => i.RequiresApproval)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(i => i.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(i => i.SortOrder)
            .IsRequired();

        builder.Property(i => i.CategoryId)
            .IsRequired();

        // FK to ApprovalWorkflow (optional)
        builder.HasOne<ApprovalWorkflow>()
            .WithMany()
            .HasForeignKey(i => i.ApprovalWorkflowId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        // Tenant isolation
        builder.Property(i => i.TenantId)
            .IsRequired();

        // Audit columns
        builder.Property(i => i.CreatedAt).IsRequired();
        builder.Property(i => i.IsDeleted).IsRequired().HasDefaultValue(false);

        // Indexes
        builder.HasIndex(i => new { i.TenantId, i.CategoryId })
            .HasDatabaseName("ix_service_catalog_items_tenant_category");

        builder.HasIndex(i => new { i.TenantId, i.SortOrder })
            .HasDatabaseName("ix_service_catalog_items_tenant_sort");
    }
}
