using AppLogica.Desk.Application.Common.Interfaces;
using AppLogica.Desk.Domain.Repositories;
using AppLogica.Desk.Domain.Sla;
using AppLogica.Desk.Infrastructure.Services;
using FluentAssertions;
using Moq;

namespace AppLogica.Desk.Tests.Unit;

public sealed class BusinessHoursCalculatorTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private readonly Mock<IBusinessHoursRepository> _repoMock = new();
    private readonly IBusinessHoursCalculator _calculator;

    public BusinessHoursCalculatorTests()
    {
        _calculator = new BusinessHoursCalculator(_repoMock.Object);
    }

    private BusinessHoursCalendar CreateGccCalendar()
    {
        return BusinessHoursCalendar.CreateGccDefault(TenantId);
    }

    private void SetupCalendar(BusinessHoursCalendar calendar, IReadOnlyList<PublicHoliday>? holidays = null)
    {
        _repoMock.Setup(r => r.GetByIdAsync(calendar.Id, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(calendar);
        _repoMock.Setup(r => r.GetHolidaysAsync(calendar.Id, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(holidays ?? Array.Empty<PublicHoliday>());
    }

    [Fact]
    public async Task CalculateDeadline_SameDayWithinBusinessHours_AddsMinutesDirectly()
    {
        // GCC calendar: 08:00-17:00 Asia/Riyadh (UTC+3)
        var calendar = CreateGccCalendar();
        SetupCalendar(calendar);

        // 2026-03-02 (Monday) 10:00 Riyadh = 07:00 UTC → add 60 business minutes
        var startUtc = new DateTime(2026, 3, 2, 7, 0, 0, DateTimeKind.Utc);

        var deadline = await _calculator.CalculateDeadlineAsync(startUtc, 60, calendar.Id, TenantId);

        // Expected: 11:00 Riyadh = 08:00 UTC
        deadline.Should().Be(new DateTime(2026, 3, 2, 8, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task CalculateDeadline_SpansEndOfDay_WrapsToNextBusinessDay()
    {
        // GCC calendar: 08:00-17:00 Asia/Riyadh (UTC+3)
        var calendar = CreateGccCalendar();
        SetupCalendar(calendar);

        // 2026-03-02 (Monday) 16:00 Riyadh = 13:00 UTC → add 120 business minutes (2 hours)
        var startUtc = new DateTime(2026, 3, 2, 13, 0, 0, DateTimeKind.Utc);

        var deadline = await _calculator.CalculateDeadlineAsync(startUtc, 120, calendar.Id, TenantId);

        // 60 min left on Monday (16:00-17:00) → 60 min carried to next day
        // Next working day: Tuesday 08:00 + 60 min = Tuesday 09:00 Riyadh = 06:00 UTC
        deadline.Should().Be(new DateTime(2026, 3, 3, 6, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task CalculateDeadline_SkipsFridayWeekend_GccCalendar()
    {
        // GCC calendar: Sun-Thu, 08:00-17:00, Asia/Riyadh (UTC+3)
        var calendar = CreateGccCalendar();
        SetupCalendar(calendar);

        // 2026-03-05 (Thursday) 16:00 Riyadh = 13:00 UTC → add 120 min
        var startUtc = new DateTime(2026, 3, 5, 13, 0, 0, DateTimeKind.Utc);

        var deadline = await _calculator.CalculateDeadlineAsync(startUtc, 120, calendar.Id, TenantId);

        // 60 min left on Thursday → Friday is off → Saturday is off → Sunday 08:00 + 60 min = 09:00 Riyadh = 06:00 UTC
        deadline.Should().Be(new DateTime(2026, 3, 8, 6, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task CalculateDeadline_SkipsPublicHoliday()
    {
        var calendar = CreateGccCalendar();
        // Monday March 2, 2026 is a holiday
        var holiday = new PublicHoliday(TenantId, calendar.Id, "Special Holiday", new DateOnly(2026, 3, 2));
        SetupCalendar(calendar, new[] { holiday });

        // 2026-03-01 (Sunday) 16:00 Riyadh = 13:00 UTC → add 120 min
        var startUtc = new DateTime(2026, 3, 1, 13, 0, 0, DateTimeKind.Utc);

        var deadline = await _calculator.CalculateDeadlineAsync(startUtc, 120, calendar.Id, TenantId);

        // 60 min left on Sunday → Monday is holiday → Tuesday 08:00 + 60 = 09:00 Riyadh = 06:00 UTC
        deadline.Should().Be(new DateTime(2026, 3, 3, 6, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task IsWithinBusinessHours_DuringWorkHours_ReturnsTrue()
    {
        var calendar = CreateGccCalendar();
        SetupCalendar(calendar);

        // Monday 10:00 Riyadh = 07:00 UTC (within 08:00-17:00)
        var utcTime = new DateTime(2026, 3, 2, 7, 0, 0, DateTimeKind.Utc);

        var result = await _calculator.IsWithinBusinessHoursAsync(utcTime, calendar.Id, TenantId);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsWithinBusinessHours_OnFriday_ReturnsFalse_ForGcc()
    {
        var calendar = CreateGccCalendar();
        SetupCalendar(calendar);

        // Friday 10:00 Riyadh = 07:00 UTC (Friday is weekend for GCC)
        var utcTime = new DateTime(2026, 3, 6, 7, 0, 0, DateTimeKind.Utc);

        var result = await _calculator.IsWithinBusinessHoursAsync(utcTime, calendar.Id, TenantId);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsWithinBusinessHours_AfterHours_ReturnsFalse()
    {
        var calendar = CreateGccCalendar();
        SetupCalendar(calendar);

        // Monday 20:00 Riyadh = 17:00 UTC (after 17:00 end)
        var utcTime = new DateTime(2026, 3, 2, 17, 0, 0, DateTimeKind.Utc);

        var result = await _calculator.IsWithinBusinessHoursAsync(utcTime, calendar.Id, TenantId);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetBusinessMinutesBetween_SameDay_ReturnsCorrectMinutes()
    {
        var calendar = CreateGccCalendar();
        SetupCalendar(calendar);

        // Monday 09:00-12:00 Riyadh = 06:00-09:00 UTC → 180 business minutes
        var start = new DateTime(2026, 3, 2, 6, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 3, 2, 9, 0, 0, DateTimeKind.Utc);

        var minutes = await _calculator.GetBusinessMinutesBetweenAsync(start, end, calendar.Id, TenantId);

        minutes.Should().Be(180);
    }

    [Fact]
    public async Task GetBusinessMinutesBetween_SpansWeekend_ExcludesNonWorkingDays()
    {
        var calendar = CreateGccCalendar();
        SetupCalendar(calendar);

        // Thursday 08:00 Riyadh (05:00 UTC) to Sunday 09:00 Riyadh (06:00 UTC)
        // Working: Thu 08:00-17:00 = 540 min, Fri off, Sat off, Sun 08:00-09:00 = 60 min = 600 total
        var start = new DateTime(2026, 3, 5, 5, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 3, 8, 6, 0, 0, DateTimeKind.Utc);

        var minutes = await _calculator.GetBusinessMinutesBetweenAsync(start, end, calendar.Id, TenantId);

        minutes.Should().Be(600);
    }

    [Fact]
    public async Task GetBusinessMinutesBetween_EndBeforeStart_ReturnsZero()
    {
        var calendar = CreateGccCalendar();
        SetupCalendar(calendar);

        var start = new DateTime(2026, 3, 3, 10, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 3, 2, 10, 0, 0, DateTimeKind.Utc);

        var minutes = await _calculator.GetBusinessMinutesBetweenAsync(start, end, calendar.Id, TenantId);

        minutes.Should().Be(0);
    }
}
