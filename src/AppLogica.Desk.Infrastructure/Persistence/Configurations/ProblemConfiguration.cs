using AppLogica.Desk.Domain.Problems;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppLogica.Desk.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="Problem"/> entity.
/// Maps to the "problems" table with appropriate column constraints and indexes.
/// </summary>
public sealed class ProblemConfiguration : IEntityTypeConfiguration<Problem>
{
    public void Configure(EntityTypeBuilder<Problem> builder)
    {
        builder.ToTable("problems");

        // Primary key
        builder.HasKey(p => p.Id);

        // Properties
        builder.Property(p => p.ProblemNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(p => p.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(p => p.Description)
            .HasMaxLength(4000);

        // Enums stored as strings
        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(p => p.Priority)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(p => p.Impact)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        // JSONB columns (jsonb on PostgreSQL, text on SQLite for tests)
        builder.Property(p => p.RootCause)
            .HasMaxLength(8000);

        builder.Property(p => p.Workaround)
            .HasMaxLength(4000);

        // LinkedIncidentIds: map to the private backing field so EF works with List<Guid>
        builder.Property<List<Guid>>("_linkedIncidentIds")
            .HasColumnName("linked_incident_ids")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<Guid>(),
                new ValueComparer<List<Guid>>(
                    (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()));

        builder.Ignore(p => p.LinkedIncidentIds);

        builder.Property(p => p.IsKnownError)
            .IsRequired()
            .HasDefaultValue(false);

        // Tenant isolation
        builder.Property(p => p.TenantId)
            .IsRequired();

        // Audit columns
        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.IsDeleted).IsRequired().HasDefaultValue(false);

        // Indexes
        builder.HasIndex(p => new { p.TenantId, p.ProblemNumber })
            .IsUnique()
            .HasDatabaseName("ix_problems_tenant_problem_number");

        builder.HasIndex(p => new { p.TenantId, p.Status })
            .HasDatabaseName("ix_problems_tenant_status");

        builder.HasIndex(p => new { p.TenantId, p.IsKnownError })
            .HasFilter("\"IsKnownError\" = true")
            .HasDatabaseName("ix_problems_tenant_known_error");

        builder.HasIndex(p => new { p.TenantId, p.AssigneeId })
            .HasDatabaseName("ix_problems_tenant_assignee");

        builder.HasIndex(p => new { p.TenantId, p.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("ix_problems_tenant_created_desc");
    }
}
