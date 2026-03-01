using AppLogica.Desk.Domain.ServiceCatalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppLogica.Desk.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="ApprovalWorkflow"/> entity.
/// Maps to the "approval_workflows" table with owned <see cref="ApprovalStep"/>
/// collection mapped to "approval_workflow_steps".
/// </summary>
public sealed class ApprovalWorkflowConfiguration : IEntityTypeConfiguration<ApprovalWorkflow>
{
    public void Configure(EntityTypeBuilder<ApprovalWorkflow> builder)
    {
        builder.ToTable("approval_workflows");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(w => w.Description)
            .HasMaxLength(2000);

        builder.Property(w => w.TimeoutMinutes)
            .IsRequired();

        // Owned collection: ApprovalStep
        builder.OwnsMany(w => w.Steps, step =>
        {
            step.ToTable("approval_workflow_steps");

            step.WithOwner().HasForeignKey("ApprovalWorkflowId");

            step.Property(s => s.StepOrder)
                .IsRequired();

            step.Property(s => s.ApproverRole)
                .IsRequired()
                .HasMaxLength(100);

            step.Property(s => s.ApprovalType)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);

            step.HasKey("ApprovalWorkflowId", nameof(ApprovalStep.StepOrder));
        });

        // Tenant isolation
        builder.Property(w => w.TenantId)
            .IsRequired();

        // Audit columns
        builder.Property(w => w.CreatedAt).IsRequired();
        builder.Property(w => w.IsDeleted).IsRequired().HasDefaultValue(false);

        // Indexes
        builder.HasIndex(w => w.TenantId)
            .HasDatabaseName("ix_approval_workflows_tenant");
    }
}
