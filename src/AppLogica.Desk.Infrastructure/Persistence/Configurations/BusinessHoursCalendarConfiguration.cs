using AppLogica.Desk.Domain.Sla;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppLogica.Desk.Infrastructure.Persistence.Configurations;

public sealed class BusinessHoursCalendarConfiguration : IEntityTypeConfiguration<BusinessHoursCalendar>
{
    public void Configure(EntityTypeBuilder<BusinessHoursCalendar> builder)
    {
        builder.ToTable("business_hours_calendars");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Profile)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(c => c.TimeZoneId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.DayStartTime)
            .IsRequired();

        builder.Property(c => c.DayEndTime)
            .IsRequired();

        builder.Property(c => c.WorkingDays)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(c => c.IsDefault)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(c => c.TenantId)
            .IsRequired();

        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.IsDeleted).IsRequired().HasDefaultValue(false);

        // One default calendar per tenant (filtered unique index)
        builder.HasIndex(c => new { c.TenantId, c.IsDefault })
            .HasDatabaseName("ix_business_hours_calendars_tenant_default")
            .HasFilter("\"IsDefault\" = true AND \"IsDeleted\" = false")
            .IsUnique();

        builder.HasIndex(c => c.TenantId)
            .HasDatabaseName("ix_business_hours_calendars_tenant");

        // Navigation to holidays
        builder.HasMany(c => c.Holidays)
            .WithOne()
            .HasForeignKey(h => h.CalendarId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
