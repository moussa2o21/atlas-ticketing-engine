using AppLogica.Desk.Domain.Sla;
using FluentAssertions;

namespace AppLogica.Desk.Tests.Unit;

/// <summary>
/// Tests for the <see cref="SlaTimer"/> domain entity, covering creation,
/// pause/resume lifecycle, and status transitions.
/// </summary>
public class SlaTimerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _incidentId = Guid.NewGuid();

    private SlaTimer CreateActiveTimer(
        TimeSpan? responseWindow = null,
        TimeSpan? resolutionWindow = null)
    {
        var now = DateTime.UtcNow;
        var responseDue = now.Add(responseWindow ?? TimeSpan.FromHours(1));
        var resolutionDue = now.Add(resolutionWindow ?? TimeSpan.FromHours(4));

        return new SlaTimer(_tenantId, _incidentId, responseDue, resolutionDue);
    }

    [Fact]
    public void SlaTimer_Create_SetsCorrectDueDates_ForCritical()
    {
        // Arrange: Critical SLA = 1h response, 4h resolution
        var now = DateTime.UtcNow;
        var responseDueAt = now.AddHours(1);
        var resolutionDueAt = now.AddHours(4);

        // Act
        var timer = new SlaTimer(_tenantId, _incidentId, responseDueAt, resolutionDueAt);

        // Assert
        timer.IncidentId.Should().Be(_incidentId);
        timer.TenantId.Should().Be(_tenantId);
        timer.ResponseDueAt.Should().BeCloseTo(responseDueAt, TimeSpan.FromSeconds(1));
        timer.ResolutionDueAt.Should().BeCloseTo(resolutionDueAt, TimeSpan.FromSeconds(1));
        timer.Status.Should().Be(SlaTimerStatus.Active);
    }

    [Fact]
    public void SlaTimer_Pause_StopsElapsedTime()
    {
        // Arrange
        var timer = CreateActiveTimer();

        // Act
        timer.Pause("Awaiting customer response");

        // Assert
        timer.Status.Should().Be(SlaTimerStatus.Paused);
        timer.PausedAt.Should().NotBeNull();
        timer.PauseReason.Should().Be("Awaiting customer response");
        timer.ElapsedBeforePause.Should().NotBeNull();
    }

    [Fact]
    public void SlaTimer_Resume_RecalculatesDueDate()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var responseDueAt = now.AddHours(1);
        var resolutionDueAt = now.AddHours(4);
        var timer = new SlaTimer(_tenantId, _incidentId, responseDueAt, resolutionDueAt);

        var originalResponseDue = timer.ResponseDueAt;
        var originalResolutionDue = timer.ResolutionDueAt;

        timer.Pause("Waiting for info");

        // Act
        timer.Resume();

        // Assert: due dates should be extended (at least as far as originals)
        timer.Status.Should().Be(SlaTimerStatus.Active);
        timer.ResponseDueAt.Should().BeOnOrAfter(originalResponseDue);
        timer.ResolutionDueAt.Should().BeOnOrAfter(originalResolutionDue);
        timer.PausedAt.Should().BeNull();
        timer.PauseReason.Should().BeNull();
        timer.ElapsedBeforePause.Should().BeNull();
    }

    [Fact]
    public void SlaTimer_MarkWarning_ChangesStatus()
    {
        // Arrange
        var timer = CreateActiveTimer();

        // Act
        timer.MarkWarning();

        // Assert
        timer.Status.Should().Be(SlaTimerStatus.Warning);
    }

    [Fact]
    public void SlaTimer_MarkBreached_ChangesStatus()
    {
        // Arrange
        var timer = CreateActiveTimer();

        // Act
        timer.MarkBreached();

        // Assert
        timer.Status.Should().Be(SlaTimerStatus.Breached);
    }

    [Fact]
    public void SlaTimer_MarkMet_ChangesStatus()
    {
        // Arrange
        var timer = CreateActiveTimer();

        // Act
        timer.MarkMet();

        // Assert
        timer.Status.Should().Be(SlaTimerStatus.Met);
    }
}
