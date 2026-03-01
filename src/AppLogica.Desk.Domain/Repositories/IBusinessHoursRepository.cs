using AppLogica.Desk.Domain.Sla;

namespace AppLogica.Desk.Domain.Repositories;

/// <summary>
/// Repository interface for business hours calendars and public holidays.
/// All methods filter by TenantId to enforce multi-tenant isolation.
/// </summary>
public interface IBusinessHoursRepository
{
    Task<BusinessHoursCalendar?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct);
    Task<BusinessHoursCalendar?> GetDefaultCalendarAsync(Guid tenantId, CancellationToken ct);
    Task<IReadOnlyList<BusinessHoursCalendar>> ListAsync(Guid tenantId, CancellationToken ct);
    Task AddAsync(BusinessHoursCalendar calendar, CancellationToken ct);
    Task UpdateAsync(BusinessHoursCalendar calendar, CancellationToken ct);
    Task AddHolidayAsync(PublicHoliday holiday, CancellationToken ct);
    Task<IReadOnlyList<PublicHoliday>> GetHolidaysAsync(Guid calendarId, Guid tenantId, CancellationToken ct);
}
