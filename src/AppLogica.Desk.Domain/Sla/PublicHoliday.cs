using AppLogica.Desk.Domain.Common;

namespace AppLogica.Desk.Domain.Sla;

/// <summary>
/// A public holiday entry associated with a <see cref="BusinessHoursCalendar"/>.
/// Holidays are non-working days for SLA calculation purposes.
/// </summary>
public sealed class PublicHoliday : Entity
{
    /// <summary>Human-readable name (e.g. "Eid Al-Fitr", "National Day").</summary>
    public string Name { get; private set; } = default!;

    /// <summary>The date of the holiday.</summary>
    public DateOnly Date { get; private set; }

    /// <summary>Whether this holiday recurs every year on the same date.</summary>
    public bool IsRecurring { get; private set; }

    /// <summary>FK to the parent business hours calendar.</summary>
    public Guid CalendarId { get; private set; }

    // EF Core
    private PublicHoliday() { }

    public PublicHoliday(Guid tenantId, Guid calendarId, string name, DateOnly date, bool isRecurring = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Holiday name is required.", nameof(name));

        TenantId = tenantId;
        CalendarId = calendarId;
        Name = name;
        Date = date;
        IsRecurring = isRecurring;
        CreatedAt = DateTime.UtcNow;
    }
}
