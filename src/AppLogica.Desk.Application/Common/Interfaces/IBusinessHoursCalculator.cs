namespace AppLogica.Desk.Application.Common.Interfaces;

/// <summary>
/// Calculates SLA deadlines accounting for business hours, weekends, and public holidays.
/// All calculations use NodaTime for timezone-aware business hours.
/// </summary>
public interface IBusinessHoursCalculator
{
    /// <summary>
    /// Calculates the deadline by adding the given number of business minutes to the start time,
    /// respecting business hours, weekends, and public holidays.
    /// </summary>
    /// <param name="startUtc">The UTC start time.</param>
    /// <param name="businessMinutes">Number of business minutes to add.</param>
    /// <param name="calendarId">The business hours calendar to use.</param>
    /// <param name="tenantId">The tenant ID for data isolation.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The calculated deadline in UTC.</returns>
    Task<DateTime> CalculateDeadlineAsync(
        DateTime startUtc,
        int businessMinutes,
        Guid calendarId,
        Guid tenantId,
        CancellationToken ct = default);

    /// <summary>
    /// Checks whether the given UTC time falls within business hours
    /// for the specified calendar.
    /// </summary>
    Task<bool> IsWithinBusinessHoursAsync(
        DateTime utcTime,
        Guid calendarId,
        Guid tenantId,
        CancellationToken ct = default);

    /// <summary>
    /// Calculates the number of elapsed business minutes between two UTC times.
    /// </summary>
    Task<int> GetBusinessMinutesBetweenAsync(
        DateTime startUtc,
        DateTime endUtc,
        Guid calendarId,
        Guid tenantId,
        CancellationToken ct = default);
}
