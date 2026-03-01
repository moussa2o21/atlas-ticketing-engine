using AppLogica.Desk.Domain.Sla;
using FluentAssertions;

namespace AppLogica.Desk.Tests.Unit;

public sealed class BusinessHoursCalendarTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    [Fact]
    public void Create_GccDefault_SetsCorrectWorkingDays()
    {
        var calendar = BusinessHoursCalendar.CreateGccDefault(TenantId);

        calendar.Profile.Should().Be(CalendarProfile.Gcc);
        calendar.TimeZoneId.Should().Be("Asia/Riyadh");
        calendar.DayStartTime.Should().Be(new TimeOnly(8, 0));
        calendar.DayEndTime.Should().Be(new TimeOnly(17, 0));
        calendar.IsDefault.Should().BeTrue();

        var workingDays = calendar.GetWorkingDays();
        workingDays.Should().BeEquivalentTo(new[]
        {
            DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday,
            DayOfWeek.Wednesday, DayOfWeek.Thursday
        });
        workingDays.Should().NotContain(DayOfWeek.Friday);
        workingDays.Should().NotContain(DayOfWeek.Saturday);
    }

    [Fact]
    public void Create_EgyptDefault_SunThuWithFriWeekend()
    {
        var calendar = BusinessHoursCalendar.CreateEgyptDefault(TenantId);

        calendar.Profile.Should().Be(CalendarProfile.Egypt);
        calendar.TimeZoneId.Should().Be("Africa/Cairo");
        calendar.DayStartTime.Should().Be(new TimeOnly(8, 0));
        calendar.DayEndTime.Should().Be(new TimeOnly(17, 0));

        var workingDays = calendar.GetWorkingDays();
        workingDays.Should().BeEquivalentTo(new[]
        {
            DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday,
            DayOfWeek.Wednesday, DayOfWeek.Thursday
        });
    }

    [Fact]
    public void Create_InternationalDefault_MonFriWithSatSunWeekend()
    {
        var calendar = BusinessHoursCalendar.CreateInternationalDefault(TenantId);

        calendar.Profile.Should().Be(CalendarProfile.International);
        calendar.TimeZoneId.Should().Be("Europe/London");
        calendar.DayStartTime.Should().Be(new TimeOnly(9, 0));
        calendar.DayEndTime.Should().Be(new TimeOnly(17, 0));

        var workingDays = calendar.GetWorkingDays();
        workingDays.Should().BeEquivalentTo(new[]
        {
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
            DayOfWeek.Thursday, DayOfWeek.Friday
        });
        workingDays.Should().NotContain(DayOfWeek.Saturday);
        workingDays.Should().NotContain(DayOfWeek.Sunday);
    }

    [Fact]
    public void Create_InvalidStartAfterEnd_Throws()
    {
        var act = () => BusinessHoursCalendar.Create(
            TenantId, "Bad Calendar", CalendarProfile.Gcc,
            "Asia/Riyadh", new TimeOnly(17, 0), new TimeOnly(8, 0),
            [DayOfWeek.Monday]);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*DayStartTime must be before DayEndTime*");
    }

    [Fact]
    public void Create_EmptyWorkingDays_Throws()
    {
        var act = () => BusinessHoursCalendar.Create(
            TenantId, "Bad Calendar", CalendarProfile.Gcc,
            "Asia/Riyadh", new TimeOnly(8, 0), new TimeOnly(17, 0),
            Array.Empty<DayOfWeek>());

        act.Should().Throw<ArgumentException>()
            .WithMessage("*At least one working day*");
    }

    [Fact]
    public void GetBusinessMinutesPerDay_Returns540_ForNineHourDay()
    {
        var calendar = BusinessHoursCalendar.CreateGccDefault(TenantId);

        calendar.GetBusinessMinutesPerDay().Should().Be(540); // 9 hours × 60
    }

    [Fact]
    public void GetBusinessMinutesPerDay_Returns480_ForEightHourDay()
    {
        var calendar = BusinessHoursCalendar.CreateInternationalDefault(TenantId);

        calendar.GetBusinessMinutesPerDay().Should().Be(480); // 8 hours × 60
    }

    [Fact]
    public void AddHoliday_AppendsToHolidaysList()
    {
        var calendar = BusinessHoursCalendar.CreateGccDefault(TenantId);
        var holiday = new PublicHoliday(
            TenantId, calendar.Id, "National Day",
            new DateOnly(2026, 9, 23), isRecurring: true);

        calendar.AddHoliday(holiday);

        calendar.Holidays.Should().HaveCount(1);
        calendar.Holidays[0].Name.Should().Be("National Day");
        calendar.Holidays[0].IsRecurring.Should().BeTrue();
    }

    [Fact]
    public void SetAsDefault_SetsIsDefaultTrue()
    {
        var calendar = BusinessHoursCalendar.Create(
            TenantId, "Custom", CalendarProfile.Gcc,
            "Asia/Riyadh", new TimeOnly(8, 0), new TimeOnly(17, 0),
            [DayOfWeek.Monday, DayOfWeek.Tuesday], isDefault: false);

        calendar.IsDefault.Should().BeFalse();

        calendar.SetAsDefault();

        calendar.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void PublicHoliday_EmptyName_Throws()
    {
        var act = () => new PublicHoliday(
            TenantId, Guid.NewGuid(), "",
            new DateOnly(2026, 1, 1));

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Holiday name is required*");
    }
}
