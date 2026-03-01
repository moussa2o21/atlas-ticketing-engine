using AppLogica.Desk.Application.Common.Interfaces;
using AppLogica.Desk.Domain.Repositories;
using AppLogica.Desk.Domain.Sla;
using NodaTime;

namespace AppLogica.Desk.Infrastructure.Services;

/// <summary>
/// Calculates SLA deadlines accounting for business hours, weekends, and public holidays
/// using NodaTime for accurate timezone conversions.
/// </summary>
public sealed class BusinessHoursCalculator : IBusinessHoursCalculator
{
    private readonly IBusinessHoursRepository _repository;

    public BusinessHoursCalculator(IBusinessHoursRepository repository)
    {
        _repository = repository;
    }

    public async Task<DateTime> CalculateDeadlineAsync(
        DateTime startUtc,
        int businessMinutes,
        Guid calendarId,
        Guid tenantId,
        CancellationToken ct = default)
    {
        var calendar = await _repository.GetByIdAsync(calendarId, tenantId, ct)
            ?? throw new InvalidOperationException($"Business hours calendar '{calendarId}' not found.");

        var holidays = await _repository.GetHolidaysAsync(calendarId, tenantId, ct);
        var holidayDates = BuildHolidayDateSet(holidays, startUtc.Year, startUtc.Year + 1);
        var workingDays = calendar.GetWorkingDays();
        var tz = DateTimeZoneProviders.Tzdb[calendar.TimeZoneId];

        // Convert start to local time in the calendar's timezone
        var instant = Instant.FromDateTimeUtc(DateTime.SpecifyKind(startUtc, DateTimeKind.Utc));
        var localStart = instant.InZone(tz).LocalDateTime;

        var remainingMinutes = businessMinutes;
        var current = localStart;

        while (remainingMinutes > 0)
        {
            var currentDate = current.Date;

            // Skip non-working days and holidays
            if (!IsWorkingDay(currentDate, workingDays, holidayDates))
            {
                current = new LocalDateTime(currentDate.PlusDays(1).Year, currentDate.PlusDays(1).Month, currentDate.PlusDays(1).Day,
                    calendar.DayStartTime.Hour, calendar.DayStartTime.Minute, 0);
                continue;
            }

            var dayStart = new LocalDateTime(currentDate.Year, currentDate.Month, currentDate.Day,
                calendar.DayStartTime.Hour, calendar.DayStartTime.Minute, 0);
            var dayEnd = new LocalDateTime(currentDate.Year, currentDate.Month, currentDate.Day,
                calendar.DayEndTime.Hour, calendar.DayEndTime.Minute, 0);

            // If current time is before the start of business, snap to start
            if (current < dayStart)
                current = dayStart;

            // If current time is at or after end of business, move to next day
            if (current >= dayEnd)
            {
                current = new LocalDateTime(currentDate.PlusDays(1).Year, currentDate.PlusDays(1).Month, currentDate.PlusDays(1).Day,
                    calendar.DayStartTime.Hour, calendar.DayStartTime.Minute, 0);
                continue;
            }

            // Calculate available minutes in this working day
            var availableMinutes = MinutesBetween(current, dayEnd);

            if (remainingMinutes <= availableMinutes)
            {
                current = current.PlusMinutes(remainingMinutes);
                remainingMinutes = 0;
            }
            else
            {
                remainingMinutes -= availableMinutes;
                current = new LocalDateTime(currentDate.PlusDays(1).Year, currentDate.PlusDays(1).Month, currentDate.PlusDays(1).Day,
                    calendar.DayStartTime.Hour, calendar.DayStartTime.Minute, 0);
            }
        }

        // Convert back to UTC
        var zonedResult = current.InZoneLeniently(tz);
        return zonedResult.ToInstant().ToDateTimeUtc();
    }

    public async Task<bool> IsWithinBusinessHoursAsync(
        DateTime utcTime,
        Guid calendarId,
        Guid tenantId,
        CancellationToken ct = default)
    {
        var calendar = await _repository.GetByIdAsync(calendarId, tenantId, ct)
            ?? throw new InvalidOperationException($"Business hours calendar '{calendarId}' not found.");

        var holidays = await _repository.GetHolidaysAsync(calendarId, tenantId, ct);
        var holidayDates = BuildHolidayDateSet(holidays, utcTime.Year, utcTime.Year);
        var workingDays = calendar.GetWorkingDays();
        var tz = DateTimeZoneProviders.Tzdb[calendar.TimeZoneId];

        var instant = Instant.FromDateTimeUtc(DateTime.SpecifyKind(utcTime, DateTimeKind.Utc));
        var local = instant.InZone(tz).LocalDateTime;
        var localDate = local.Date;

        if (!IsWorkingDay(localDate, workingDays, holidayDates))
            return false;

        var localTime = new TimeOnly(local.Hour, local.Minute, local.Second);
        return localTime >= calendar.DayStartTime && localTime < calendar.DayEndTime;
    }

    public async Task<int> GetBusinessMinutesBetweenAsync(
        DateTime startUtc,
        DateTime endUtc,
        Guid calendarId,
        Guid tenantId,
        CancellationToken ct = default)
    {
        if (endUtc <= startUtc)
            return 0;

        var calendar = await _repository.GetByIdAsync(calendarId, tenantId, ct)
            ?? throw new InvalidOperationException($"Business hours calendar '{calendarId}' not found.");

        var holidays = await _repository.GetHolidaysAsync(calendarId, tenantId, ct);
        var holidayDates = BuildHolidayDateSet(holidays, startUtc.Year, endUtc.Year);
        var workingDays = calendar.GetWorkingDays();
        var tz = DateTimeZoneProviders.Tzdb[calendar.TimeZoneId];

        var startInstant = Instant.FromDateTimeUtc(DateTime.SpecifyKind(startUtc, DateTimeKind.Utc));
        var endInstant = Instant.FromDateTimeUtc(DateTime.SpecifyKind(endUtc, DateTimeKind.Utc));
        var localStart = startInstant.InZone(tz).LocalDateTime;
        var localEnd = endInstant.InZone(tz).LocalDateTime;

        var totalMinutes = 0;
        var current = localStart;

        while (current < localEnd)
        {
            var currentDate = current.Date;

            if (!IsWorkingDay(currentDate, workingDays, holidayDates))
            {
                current = new LocalDateTime(currentDate.PlusDays(1).Year, currentDate.PlusDays(1).Month, currentDate.PlusDays(1).Day,
                    calendar.DayStartTime.Hour, calendar.DayStartTime.Minute, 0);
                continue;
            }

            var dayStart = new LocalDateTime(currentDate.Year, currentDate.Month, currentDate.Day,
                calendar.DayStartTime.Hour, calendar.DayStartTime.Minute, 0);
            var dayEnd = new LocalDateTime(currentDate.Year, currentDate.Month, currentDate.Day,
                calendar.DayEndTime.Hour, calendar.DayEndTime.Minute, 0);

            var effectiveStart = current < dayStart ? dayStart : current;
            var effectiveEnd = localEnd < dayEnd ? localEnd : dayEnd;

            if (effectiveStart < effectiveEnd)
            {
                totalMinutes += MinutesBetween(effectiveStart, effectiveEnd);
            }

            current = new LocalDateTime(currentDate.PlusDays(1).Year, currentDate.PlusDays(1).Month, currentDate.PlusDays(1).Day,
                calendar.DayStartTime.Hour, calendar.DayStartTime.Minute, 0);
        }

        return totalMinutes;
    }

    /// <summary>
    /// Computes the total minutes between two LocalDateTime values.
    /// Uses NodaTime's nanosecond-of-day for same-day calculations, or falls back to
    /// converting through ticks for cross-day spans.
    /// </summary>
    private static int MinutesBetween(LocalDateTime start, LocalDateTime end)
    {
        var startTicks = start.Date.AtStartOfDayInZone(DateTimeZone.Utc).ToInstant().ToUnixTimeTicks()
            + start.NanosecondOfDay / 100;
        var endTicks = end.Date.AtStartOfDayInZone(DateTimeZone.Utc).ToInstant().ToUnixTimeTicks()
            + end.NanosecondOfDay / 100;
        return (int)((endTicks - startTicks) / TimeSpan.TicksPerMinute);
    }

    private static bool IsWorkingDay(LocalDate date, DayOfWeek[] workingDays, HashSet<LocalDate> holidayDates)
    {
        if (holidayDates.Contains(date))
            return false;

        var dayOfWeek = date.DayOfWeek switch
        {
            IsoDayOfWeek.Monday => DayOfWeek.Monday,
            IsoDayOfWeek.Tuesday => DayOfWeek.Tuesday,
            IsoDayOfWeek.Wednesday => DayOfWeek.Wednesday,
            IsoDayOfWeek.Thursday => DayOfWeek.Thursday,
            IsoDayOfWeek.Friday => DayOfWeek.Friday,
            IsoDayOfWeek.Saturday => DayOfWeek.Saturday,
            IsoDayOfWeek.Sunday => DayOfWeek.Sunday,
            _ => throw new InvalidOperationException($"Unknown day of week: {date.DayOfWeek}")
        };

        return workingDays.Contains(dayOfWeek);
    }

    private static HashSet<LocalDate> BuildHolidayDateSet(
        IReadOnlyList<PublicHoliday> holidays, int startYear, int endYear)
    {
        var dates = new HashSet<LocalDate>();

        foreach (var holiday in holidays)
        {
            if (holiday.IsRecurring)
            {
                for (var year = startYear; year <= endYear; year++)
                {
                    dates.Add(new LocalDate(year, holiday.Date.Month, holiday.Date.Day));
                }
            }
            else
            {
                dates.Add(new LocalDate(holiday.Date.Year, holiday.Date.Month, holiday.Date.Day));
            }
        }

        return dates;
    }
}
