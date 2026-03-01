using AppLogica.Desk.Application.Common.Interfaces;
using AppLogica.Desk.Application.Sla.Services;
using AppLogica.Desk.Domain.Incidents;
using AppLogica.Desk.Domain.Repositories;
using AppLogica.Desk.Domain.Sla;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace AppLogica.Desk.Tests.Unit;

public sealed class SlaTimerServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private readonly Mock<ISlaRepository> _slaRepoMock = new();
    private readonly Mock<IBusinessHoursRepository> _bhRepoMock = new();
    private readonly Mock<IBusinessHoursCalculator> _calcMock = new();
    private readonly SlaTimerService _service;

    public SlaTimerServiceTests()
    {
        _service = new SlaTimerService(
            _slaRepoMock.Object,
            _bhRepoMock.Object,
            _calcMock.Object,
            Mock.Of<ILogger<SlaTimerService>>());
    }

    [Fact]
    public async Task CreateTimer_WithBusinessHours_UsesCalculator()
    {
        // Arrange
        var policy = new SlaPolicy(TenantId, "Standard", new[]
        {
            new SlaTarget(Priority.High, 30, 240)
        });
        _slaRepoMock.Setup(r => r.GetPolicyByPriorityAsync(Priority.High, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(policy);

        var calendar = BusinessHoursCalendar.CreateGccDefault(TenantId);
        _bhRepoMock.Setup(r => r.GetDefaultCalendarAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(calendar);

        var responseDue = DateTime.UtcNow.AddMinutes(30);
        var resolutionDue = DateTime.UtcNow.AddHours(4);

        _calcMock.Setup(c => c.CalculateDeadlineAsync(
                It.IsAny<DateTime>(), 30, calendar.Id, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDue);

        _calcMock.Setup(c => c.CalculateDeadlineAsync(
                It.IsAny<DateTime>(), 240, calendar.Id, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resolutionDue);

        // Act
        var timer = await _service.CreateTimerAsync(Guid.NewGuid(), Priority.High, TenantId);

        // Assert
        timer.Should().NotBeNull();
        timer!.ResponseDueAt.Should().Be(responseDue);
        timer.ResolutionDueAt.Should().Be(resolutionDue);
        _slaRepoMock.Verify(r => r.AddTimerAsync(It.IsAny<SlaTimer>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateTimer_WithoutCalendar_FallsBackTo24x7()
    {
        var policy = new SlaPolicy(TenantId, "Standard", new[]
        {
            new SlaTarget(Priority.Medium, 60, 480)
        });
        _slaRepoMock.Setup(r => r.GetPolicyByPriorityAsync(Priority.Medium, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(policy);

        _bhRepoMock.Setup(r => r.GetDefaultCalendarAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BusinessHoursCalendar?)null);

        var timer = await _service.CreateTimerAsync(Guid.NewGuid(), Priority.Medium, TenantId);

        timer.Should().NotBeNull();
        // Without calendar, deadlines are raw minutes from now
        _calcMock.Verify(c => c.CalculateDeadlineAsync(
            It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateTimer_NoPolicyFound_ReturnsNull()
    {
        _slaRepoMock.Setup(r => r.GetPolicyByPriorityAsync(Priority.Low, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SlaPolicy?)null);

        var timer = await _service.CreateTimerAsync(Guid.NewGuid(), Priority.Low, TenantId);

        timer.Should().BeNull();
    }

    [Fact]
    public async Task PauseTimer_WithReason_PausesCorrectly()
    {
        var incidentId = Guid.NewGuid();
        var timer = new SlaTimer(TenantId, incidentId,
            DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(4));

        _slaRepoMock.Setup(r => r.GetTimerByIncidentIdAsync(incidentId, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(timer);

        await _service.PauseTimerAsync(incidentId, TenantId, SlaPauseReason.AwaitingCustomer, "Waiting for logs");

        timer.Status.Should().Be(SlaTimerStatus.Paused);
        timer.PauseReason.Should().Contain("AwaitingCustomer");
        timer.PauseReason.Should().Contain("Waiting for logs");
    }

    [Fact]
    public async Task ResumeTimer_ExtendsDueDates()
    {
        var incidentId = Guid.NewGuid();
        var timer = new SlaTimer(TenantId, incidentId,
            DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(4));

        timer.Pause("Test pause");
        var originalResolutionDue = timer.ResolutionDueAt;

        _slaRepoMock.Setup(r => r.GetTimerByIncidentIdAsync(incidentId, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(timer);

        // Small delay to ensure paused duration > 0
        await Task.Delay(10);

        await _service.ResumeTimerAsync(incidentId, TenantId);

        timer.Status.Should().Be(SlaTimerStatus.Active);
        timer.ResolutionDueAt.Should().BeAfter(originalResolutionDue);
    }

    [Fact]
    public async Task RecalculateOnPriorityChange_VoidsOldAndCreatesNew()
    {
        var incidentId = Guid.NewGuid();
        var oldTimer = new SlaTimer(TenantId, incidentId,
            DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(4));

        _slaRepoMock.Setup(r => r.GetTimerByIncidentIdAsync(incidentId, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(oldTimer);

        var newPolicy = new SlaPolicy(TenantId, "Critical SLA", new[]
        {
            new SlaTarget(Priority.Critical, 15, 60)
        });
        _slaRepoMock.Setup(r => r.GetPolicyByPriorityAsync(Priority.Critical, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newPolicy);

        _bhRepoMock.Setup(r => r.GetDefaultCalendarAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BusinessHoursCalendar?)null);

        await _service.RecalculateOnPriorityChangeAsync(incidentId, Priority.Critical, TenantId);

        oldTimer.Status.Should().Be(SlaTimerStatus.Voided);
        _slaRepoMock.Verify(r => r.UpdateTimerAsync(oldTimer, It.IsAny<CancellationToken>()), Times.Once);
        _slaRepoMock.Verify(r => r.AddTimerAsync(It.IsAny<SlaTimer>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
