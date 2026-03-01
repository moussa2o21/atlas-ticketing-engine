using AppLogica.Desk.Domain.Repositories;
using AppLogica.Desk.Domain.Sla;
using Microsoft.EntityFrameworkCore;

namespace AppLogica.Desk.Infrastructure.Persistence.Repositories;

public sealed class BusinessHoursRepository : IBusinessHoursRepository
{
    private readonly DeskDbContext _dbContext;

    public BusinessHoursRepository(DeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<BusinessHoursCalendar?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct)
    {
        return await _dbContext.BusinessHoursCalendars
            .Include(c => c.Holidays)
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId, ct);
    }

    public async Task<BusinessHoursCalendar?> GetDefaultCalendarAsync(Guid tenantId, CancellationToken ct)
    {
        return await _dbContext.BusinessHoursCalendars
            .Include(c => c.Holidays)
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.IsDefault, ct);
    }

    public async Task<IReadOnlyList<BusinessHoursCalendar>> ListAsync(Guid tenantId, CancellationToken ct)
    {
        return await _dbContext.BusinessHoursCalendars
            .Include(c => c.Holidays)
            .Where(c => c.TenantId == tenantId)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);
    }

    public async Task AddAsync(BusinessHoursCalendar calendar, CancellationToken ct)
    {
        await _dbContext.BusinessHoursCalendars.AddAsync(calendar, ct);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(BusinessHoursCalendar calendar, CancellationToken ct)
    {
        _dbContext.BusinessHoursCalendars.Update(calendar);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task AddHolidayAsync(PublicHoliday holiday, CancellationToken ct)
    {
        await _dbContext.PublicHolidays.AddAsync(holiday, ct);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<PublicHoliday>> GetHolidaysAsync(Guid calendarId, Guid tenantId, CancellationToken ct)
    {
        return await _dbContext.PublicHolidays
            .Where(h => h.CalendarId == calendarId && h.TenantId == tenantId)
            .OrderBy(h => h.Date)
            .ToListAsync(ct);
    }
}
