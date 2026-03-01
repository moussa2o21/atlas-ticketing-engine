using AppLogica.Desk.Domain.Common;

namespace AppLogica.Desk.Domain.Sla;

/// <summary>
/// Per-tenant business hours calendar defining working days, hours, timezone, and holidays.
/// Supports MENA working patterns (GCC Fri-Sat weekend, Egypt Fri weekend, International Sat-Sun weekend).
/// </summary>
public sealed class BusinessHoursCalendar : Entity
{
    private readonly List<PublicHoliday> _holidays = [];

    /// <summary>Human-readable name (e.g. "GCC Standard", "Egypt Standard").</summary>
    public string Name { get; private set; } = default!;

    /// <summary>Predefined calendar profile (GCC, Egypt, International).</summary>
    public CalendarProfile Profile { get; private set; }

    /// <summary>IANA timezone identifier (e.g. "Asia/Riyadh", "Africa/Cairo", "Europe/London").</summary>
    public string TimeZoneId { get; private set; } = default!;

    /// <summary>Start of the working day (e.g. 08:00).</summary>
    public TimeOnly DayStartTime { get; private set; }

    /// <summary>End of the working day (e.g. 17:00).</summary>
    public TimeOnly DayEndTime { get; private set; }

    /// <summary>Working days as a comma-separated list (e.g. "Sunday,Monday,Tuesday,Wednesday,Thursday").</summary>
    public string WorkingDays { get; private set; } = default!;

    /// <summary>Whether this is the default calendar for the tenant.</summary>
    public bool IsDefault { get; private set; }

    /// <summary>Public holidays associated with this calendar.</summary>
    public IReadOnlyList<PublicHoliday> Holidays => _holidays.AsReadOnly();

    // EF Core
    private BusinessHoursCalendar() { }

    /// <summary>
    /// Creates a new business hours calendar.
    /// </summary>
    public static BusinessHoursCalendar Create(
        Guid tenantId,
        string name,
        CalendarProfile profile,
        string timeZoneId,
        TimeOnly dayStartTime,
        TimeOnly dayEndTime,
        DayOfWeek[] workingDays,
        bool isDefault = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Calendar name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(timeZoneId))
            throw new ArgumentException("TimeZoneId is required.", nameof(timeZoneId));
        if (dayStartTime >= dayEndTime)
            throw new ArgumentException("DayStartTime must be before DayEndTime.");
        if (workingDays.Length == 0)
            throw new ArgumentException("At least one working day is required.", nameof(workingDays));

        return new BusinessHoursCalendar
        {
            TenantId = tenantId,
            Name = name,
            Profile = profile,
            TimeZoneId = timeZoneId,
            DayStartTime = dayStartTime,
            DayEndTime = dayEndTime,
            WorkingDays = string.Join(",", workingDays.Select(d => d.ToString())),
            IsDefault = isDefault,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Returns the working days as a <see cref="DayOfWeek"/> array.
    /// </summary>
    public DayOfWeek[] GetWorkingDays()
    {
        return WorkingDays
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(d => Enum.Parse<DayOfWeek>(d))
            .ToArray();
    }

    /// <summary>
    /// Returns the number of business minutes in a single working day.
    /// </summary>
    public int GetBusinessMinutesPerDay()
    {
        return (int)(DayEndTime - DayStartTime).TotalMinutes;
    }

    /// <summary>
    /// Adds a public holiday to this calendar.
    /// </summary>
    public void AddHoliday(PublicHoliday holiday)
    {
        _holidays.Add(holiday);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets this calendar as the default for the tenant.
    /// </summary>
    public void SetAsDefault()
    {
        IsDefault = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Removes the default flag from this calendar.
    /// </summary>
    public void ClearDefault()
    {
        IsDefault = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a default GCC calendar (Sun-Thu, 08:00-17:00, Asia/Riyadh).
    /// </summary>
    public static BusinessHoursCalendar CreateGccDefault(Guid tenantId) =>
        Create(tenantId, "GCC Standard", CalendarProfile.Gcc, "Asia/Riyadh",
            new TimeOnly(8, 0), new TimeOnly(17, 0),
            [DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday],
            isDefault: true);

    /// <summary>
    /// Creates a default Egypt calendar (Sun-Thu, 08:00-17:00, Africa/Cairo).
    /// </summary>
    public static BusinessHoursCalendar CreateEgyptDefault(Guid tenantId) =>
        Create(tenantId, "Egypt Standard", CalendarProfile.Egypt, "Africa/Cairo",
            new TimeOnly(8, 0), new TimeOnly(17, 0),
            [DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday],
            isDefault: true);

    /// <summary>
    /// Creates a default International calendar (Mon-Fri, 09:00-17:00, Europe/London).
    /// </summary>
    public static BusinessHoursCalendar CreateInternationalDefault(Guid tenantId) =>
        Create(tenantId, "International Standard", CalendarProfile.International, "Europe/London",
            new TimeOnly(9, 0), new TimeOnly(17, 0),
            [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday],
            isDefault: true);
}
