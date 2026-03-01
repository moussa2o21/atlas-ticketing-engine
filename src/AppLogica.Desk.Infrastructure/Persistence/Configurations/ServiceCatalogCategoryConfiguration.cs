using AppLogica.Desk.Domain.ServiceCatalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppLogica.Desk.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="ServiceCatalogCategory"/> entity.
/// Maps to the "service_catalog_categories" table.
/// </summary>
public sealed class ServiceCatalogCategoryConfiguration : IEntityTypeConfiguration<ServiceCatalogCategory>
{
    public void Configure(EntityTypeBuilder<ServiceCatalogCategory> builder)
    {
        builder.ToTable("service_catalog_categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Description)
            .HasMaxLength(2000);

        builder.Property(c => c.SortOrder)
            .IsRequired();

        builder.Property(c => c.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Self-referencing hierarchy
        builder.HasOne<ServiceCatalogCategory>()
            .WithMany()
            .HasForeignKey(c => c.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        // Navigation to items
        builder.HasMany(c => c.Items)
            .WithOne()
            .HasForeignKey(i => i.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Tenant isolation
        builder.Property(c => c.TenantId)
            .IsRequired();

        // Audit columns
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.IsDeleted).IsRequired().HasDefaultValue(false);

        // Indexes
        builder.HasIndex(c => new { c.TenantId, c.ParentCategoryId })
            .HasDatabaseName("ix_service_catalog_categories_tenant_parent");

        builder.HasIndex(c => new { c.TenantId, c.SortOrder })
            .HasDatabaseName("ix_service_catalog_categories_tenant_sort");
    }
}
