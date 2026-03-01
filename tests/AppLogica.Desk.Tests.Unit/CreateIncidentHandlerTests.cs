using AppLogica.Desk.Application.Common.Interfaces;
using AppLogica.Desk.Application.Incidents.Commands.CreateIncident;
using AppLogica.Desk.Domain.Incidents;
using AppLogica.Desk.Domain.Repositories;
using AppLogica.Desk.Domain.Sla;
using FluentAssertions;
using Moq;

namespace AppLogica.Desk.Tests.Unit;

/// <summary>
/// Tests for <see cref="CreateIncidentCommandHandler"/> verifying ticket number generation,
/// SLA timer creation, tenant isolation, and validation rejection.
/// </summary>
public class CreateIncidentHandlerTests
{
    private readonly Mock<IIncidentRepository> _incidentRepoMock = new();
    private readonly Mock<ISlaRepository> _slaRepoMock = new();
    private readonly Mock<ITenantContext> _tenantContextMock = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    private CreateIncidentCommandHandler CreateHandler()
    {
        _tenantContextMock.Setup(x => x.TenantId).Returns(_tenantId);

        return new CreateIncidentCommandHandler(
            _incidentRepoMock.Object,
            _slaRepoMock.Object,
            _tenantContextMock.Object);
    }

    [Fact]
    public async Task CreateIncidentHandler_GeneratesTicketNumber()
    {
        // Arrange
        _incidentRepoMock
            .Setup(r => r.GetNextTicketSequenceAsync(_tenantId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _slaRepoMock
            .Setup(r => r.GetPolicyByPriorityAsync(It.IsAny<Priority>(), _tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SlaPolicy?)null);

        var handler = CreateHandler();
        var command = new CreateIncidentCommand(
            "Server Down", "Production server is not responding",
            Impact.Enterprise, Urgency.Immediate, null);

        // Act
        var incidentId = await handler.Handle(command, CancellationToken.None);

        // Assert
        incidentId.Should().NotBeEmpty();

        // Verify the incident was added with the expected ticket number format
        _incidentRepoMock.Verify(r => r.AddAsync(
            It.Is<Incident>(i => i.TicketNumber.StartsWith("INC-") && i.TicketNumber.EndsWith("-00001")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateIncidentHandler_CreatesSlaTimer_ForPriority_Critical()
    {
        // Arrange
        _incidentRepoMock
            .Setup(r => r.GetNextTicketSequenceAsync(_tenantId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var criticalTarget = new SlaTarget(Priority.Critical, ResponseMinutes: 60, ResolutionMinutes: 240);
        var slaPolicy = new SlaPolicy(_tenantId, "Standard SLA", new[] { criticalTarget });

        _slaRepoMock
            .Setup(r => r.GetPolicyByPriorityAsync(Priority.Critical, _tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(slaPolicy);

        var handler = CreateHandler();
        var command = new CreateIncidentCommand(
            "Critical Issue", "Enterprise-wide outage",
            Impact.Enterprise, Urgency.Immediate, null);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert: SLA timer should have been created
        _slaRepoMock.Verify(r => r.AddTimerAsync(
            It.Is<SlaTimer>(t => t.TenantId == _tenantId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateIncidentHandler_TenantId_FromContext_NotBody()
    {
        // Arrange
        _incidentRepoMock
            .Setup(r => r.GetNextTicketSequenceAsync(_tenantId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _slaRepoMock
            .Setup(r => r.GetPolicyByPriorityAsync(It.IsAny<Priority>(), _tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SlaPolicy?)null);

        var handler = CreateHandler();
        var command = new CreateIncidentCommand(
            "Test", "Test description",
            Impact.Department, Urgency.High, null);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert: The incident should have been added with the TenantId from the context, not from the body
        _incidentRepoMock.Verify(r => r.AddAsync(
            It.Is<Incident>(i => i.TenantId == _tenantId),
            It.IsAny<CancellationToken>()), Times.Once);

        // Verify TenantId was read from the context
        _tenantContextMock.Verify(x => x.TenantId, Times.AtLeastOnce);
    }

    [Fact]
    public async Task CreateIncidentHandler_ThrowsOnEmptyTitle()
    {
        // Arrange: The CreateIncidentCommandValidator rejects empty titles.
        // This test validates the validator directly, since the handler itself
        // does not perform validation (that's done by ValidationBehaviour).
        var validator = new CreateIncidentCommandValidator();
        var command = new CreateIncidentCommand(
            "", "Some description",
            Impact.Department, Urgency.Normal, null);

        // Act
        var result = await validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }
}
