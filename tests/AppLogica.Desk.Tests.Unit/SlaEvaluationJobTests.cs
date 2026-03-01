using AppLogica.Desk.Application.Sla.Jobs;
using AppLogica.Desk.Domain.Repositories;
using AppLogica.Desk.Domain.Sla;
using AppLogica.Desk.Domain.Sla.Events;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

namespace AppLogica.Desk.Tests.Unit;

/// <summary>
/// Tests for <see cref="SlaEvaluationJob"/> verifying warning/breach detection
/// and idempotent behavior on already-breached timers.
/// </summary>
public class SlaEvaluationJobTests
{
    private readonly Mock<ISlaRepository> _slaRepoMock = new();
    private readonly Mock<IPublisher> _publisherMock = new();
    private readonly Mock<ILogger<SlaEvaluationJob>> _loggerMock = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    private SlaEvaluationJob CreateJob()
    {
        return new SlaEvaluationJob(
            _slaRepoMock.Object,
            _publisherMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task SlaEvaluationJob_PublishesWarning_At80PercentElapsed()
    {
        // Arrange: Create a timer that is 85% elapsed (past the 80% threshold)
        var now = DateTime.UtcNow;
        var createdAt = now.AddHours(-8.5);   // created 8.5 hours ago
        var responseDue = now.AddHours(1);
        var resolutionDue = now.AddHours(1.5); // total window = 10h, 8.5h elapsed = 85%

        var timer = new SlaTimer(_tenantId, Guid.NewGuid(), responseDue, resolutionDue);
        // We need to set CreatedAt to the past to simulate elapsed time
        timer.CreatedAt = createdAt;

        _slaRepoMock
            .Setup(r => r.GetActiveTimersAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SlaTimer> { timer });

        var job = CreateJob();

        // Act
        await job.ExecuteAsync(_tenantId);

        // Assert: Timer should be marked as Warning and event should be published
        timer.Status.Should().Be(SlaTimerStatus.Warning);
        _publisherMock.Verify(
            p => p.Publish(It.IsAny<SlaWarningEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _slaRepoMock.Verify(
            r => r.UpdateTimerAsync(timer, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SlaEvaluationJob_PublishesBreach_WhenPastDue()
    {
        // Arrange: Create a timer that is past its resolution due date
        var now = DateTime.UtcNow;
        var createdAt = now.AddHours(-5);
        var responseDue = now.AddHours(-4);    // response already past
        var resolutionDue = now.AddHours(-1);  // resolution already past

        var timer = new SlaTimer(_tenantId, Guid.NewGuid(), responseDue, resolutionDue);
        timer.CreatedAt = createdAt;

        _slaRepoMock
            .Setup(r => r.GetActiveTimersAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SlaTimer> { timer });

        var job = CreateJob();

        // Act
        await job.ExecuteAsync(_tenantId);

        // Assert: Timer should be marked as Breached and breach event should be published
        timer.Status.Should().Be(SlaTimerStatus.Breached);
        _publisherMock.Verify(
            p => p.Publish(It.IsAny<SlaBreachedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _slaRepoMock.Verify(
            r => r.UpdateTimerAsync(timer, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SlaEvaluationJob_Idempotent_DoesNotDoublePublish()
    {
        // Arrange: Create a timer that is already breached
        // The job should not try to breach it again
        var now = DateTime.UtcNow;
        var timer = new SlaTimer(_tenantId, Guid.NewGuid(), now.AddHours(-2), now.AddHours(-1));
        timer.CreatedAt = now.AddHours(-5);

        // Mark as breached first (simulating it was already processed)
        timer.MarkBreached();

        // GetActiveTimersAsync returns Active or Warning timers only in the real repo,
        // but even if it somehow returns a Breached timer, the job should be idempotent.
        _slaRepoMock
            .Setup(r => r.GetActiveTimersAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SlaTimer> { timer });

        var job = CreateJob();

        // Act
        await job.ExecuteAsync(_tenantId);

        // Assert: No publish calls should be made since timer is already Breached
        _publisherMock.Verify(
            p => p.Publish(It.IsAny<SlaBreachedEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _publisherMock.Verify(
            p => p.Publish(It.IsAny<SlaWarningEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _slaRepoMock.Verify(
            r => r.UpdateTimerAsync(It.IsAny<SlaTimer>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
