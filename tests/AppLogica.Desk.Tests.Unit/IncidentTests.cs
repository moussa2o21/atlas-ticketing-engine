using AppLogica.Desk.Domain.Incidents;
using AppLogica.Desk.Domain.Incidents.Events;
using FluentAssertions;

namespace AppLogica.Desk.Tests.Unit;

public class IncidentTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _createdBy = Guid.NewGuid();

    private Incident CreateTestIncident(
        Impact impact = Impact.Department,
        Urgency urgency = Urgency.High)
    {
        return Incident.Create(
            _tenantId, "Test Incident", "Test description",
            impact, urgency, "INC-2026-00001", _createdBy);
    }

    [Fact]
    public void CanCreate_Incident_WithValidData()
    {
        var incident = CreateTestIncident();

        incident.Title.Should().Be("Test Incident");
        incident.Description.Should().Be("Test description");
        incident.TicketNumber.Should().Be("INC-2026-00001");
        incident.Status.Should().Be(IncidentStatus.New);
        incident.TenantId.Should().Be(_tenantId);
    }

    [Fact]
    public void CanTransition_NewToAssigned_WhenAssigneeProvided()
    {
        var incident = CreateTestIncident();
        var assigneeId = Guid.NewGuid();

        incident.Assign(assigneeId, Guid.NewGuid());

        incident.Status.Should().Be(IncidentStatus.Assigned);
        incident.AssigneeId.Should().Be(assigneeId);
    }

    [Fact]
    public void CannotTransition_ClosedToClosed()
    {
        var incident = CreateTestIncident();
        var assigneeId = Guid.NewGuid();
        incident.Assign(assigneeId, Guid.NewGuid());
        incident.StartProgress(Guid.NewGuid());
        incident.Resolve("Fixed", Guid.NewGuid());
        incident.Close(Guid.NewGuid());

        var act = () => incident.Close(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void PriorityMatrix_Critical_WhenImpactEnterpriseAndUrgencyImmediate()
    {
        var incident = Incident.Create(
            _tenantId, "Critical test", "desc",
            Impact.Enterprise, Urgency.Immediate, "INC-2026-00002", _createdBy);

        incident.Priority.Should().Be(Priority.Critical);
    }

    [Fact]
    public void DomainEvent_Raised_OnAssignment()
    {
        var incident = CreateTestIncident();
        incident.ClearDomainEvents(); // clear the created event

        incident.Assign(Guid.NewGuid(), Guid.NewGuid());

        incident.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<IncidentAssignedEvent>();
    }

    [Fact]
    public void DomainEvent_Raised_OnResolution()
    {
        var incident = CreateTestIncident();
        incident.Assign(Guid.NewGuid(), Guid.NewGuid());
        incident.StartProgress(Guid.NewGuid());
        incident.ClearDomainEvents();

        incident.Resolve("Root cause found", Guid.NewGuid());

        incident.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<IncidentResolvedEvent>();
    }

    [Fact]
    public void ResolutionNotes_Required_ToResolve()
    {
        var incident = CreateTestIncident();
        incident.Assign(Guid.NewGuid(), Guid.NewGuid());
        incident.StartProgress(Guid.NewGuid());

        var act = () => incident.Resolve("", Guid.NewGuid());

        act.Should().Throw<ArgumentException>();
    }
}
