using AppLogica.Desk.Domain.Incidents;
using AppLogica.Desk.Domain.Sla;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppLogica.Desk.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="SlaPolicy"/> entity.
/// Maps to the "sla_policies" table. SlaTarget value objects are stored as an owned collection.
/// </summary>
public sealed class SlaPolicyConfiguration : IEntityTypeConfiguration<SlaPolicy>
{
    public void Configure(EntityTypeBuilder<SlaPolicy> builder)
    {
        builder.ToTable("sla_policies");

        // Primary key
        builder.HasKey(p => p.Id);

        // Properties
        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.TenantId)
            .IsRequired();

        // Audit columns
        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.IsDeleted).IsRequired().HasDefaultValue(false);

        // Owned collection: SlaTarget value objects stored in a separate table
        builder.OwnsMany(p => p.Targets, targetsBuilder =>
        {
            targetsBuilder.ToTable("sla_policy_targets");

            targetsBuilder.WithOwner().HasForeignKey("SlaPolicyId");

            targetsBuilder.Property(t => t.Priority)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);

            targetsBuilder.Property(t => t.ResponseMinutes)
                .IsRequired();

            targetsBuilder.Property(t => t.ResolutionMinutes)
                .IsRequired();
        });
    }
}
