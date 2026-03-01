using AppLogica.Desk.Domain.Sla;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppLogica.Desk.Infrastructure.Persistence.Configurations;

public sealed class PublicHolidayConfiguration : IEntityTypeConfiguration<PublicHoliday>
{
    public void Configure(EntityTypeBuilder<PublicHoliday> builder)
    {
        builder.ToTable("public_holidays");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(h => h.Date)
            .IsRequired();

        builder.Property(h => h.IsRecurring)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(h => h.CalendarId)
            .IsRequired();

        builder.Property(h => h.TenantId)
            .IsRequired();

        builder.Property(h => h.CreatedAt).IsRequired();
        builder.Property(h => h.IsDeleted).IsRequired().HasDefaultValue(false);

        builder.HasIndex(h => new { h.CalendarId, h.Date })
            .HasDatabaseName("ix_public_holidays_calendar_date");

        builder.HasIndex(h => h.TenantId)
            .HasDatabaseName("ix_public_holidays_tenant");
    }
}
