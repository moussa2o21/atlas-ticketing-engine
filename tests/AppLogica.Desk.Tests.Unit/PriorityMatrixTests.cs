using AppLogica.Desk.Domain.Incidents;
using FluentAssertions;

namespace AppLogica.Desk.Tests.Unit;

/// <summary>
/// Tests every combination of the ITIL Impact x Urgency priority matrix
/// as implemented in <see cref="Incident.Create"/>.
/// </summary>
public class PriorityMatrixTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _createdBy = Guid.NewGuid();

    private Incident CreateIncident(Impact impact, Urgency urgency)
    {
        return Incident.Create(
            _tenantId,
            "Priority Matrix Test",
            "Testing priority calculation",
            impact,
            urgency,
            $"INC-2026-{Guid.NewGuid().ToString()[..5]}",
            _createdBy);
    }

    [Fact]
    public void Enterprise_Immediate_ShouldBe_Critical()
    {
        var incident = CreateIncident(Impact.Enterprise, Urgency.Immediate);
        incident.Priority.Should().Be(Priority.Critical);
    }

    [Fact]
    public void Enterprise_High_ShouldBe_High()
    {
        var incident = CreateIncident(Impact.Enterprise, Urgency.High);
        incident.Priority.Should().Be(Priority.High);
    }

    [Fact]
    public void Enterprise_Normal_ShouldBe_Medium()
    {
        var incident = CreateIncident(Impact.Enterprise, Urgency.Normal);
        incident.Priority.Should().Be(Priority.Medium);
    }

    [Fact]
    public void Enterprise_Low_ShouldBe_Low()
    {
        var incident = CreateIncident(Impact.Enterprise, Urgency.Low);
        incident.Priority.Should().Be(Priority.Low);
    }

    [Fact]
    public void Department_Immediate_ShouldBe_High()
    {
        var incident = CreateIncident(Impact.Department, Urgency.Immediate);
        incident.Priority.Should().Be(Priority.High);
    }

    [Fact]
    public void Department_High_ShouldBe_Medium()
    {
        var incident = CreateIncident(Impact.Department, Urgency.High);
        incident.Priority.Should().Be(Priority.Medium);
    }

    [Fact]
    public void Department_Normal_ShouldBe_Low()
    {
        var incident = CreateIncident(Impact.Department, Urgency.Normal);
        incident.Priority.Should().Be(Priority.Low);
    }

    [Fact]
    public void Department_Low_ShouldBe_Low()
    {
        var incident = CreateIncident(Impact.Department, Urgency.Low);
        incident.Priority.Should().Be(Priority.Low);
    }

    [Fact]
    public void Team_Immediate_ShouldBe_Medium()
    {
        var incident = CreateIncident(Impact.Team, Urgency.Immediate);
        incident.Priority.Should().Be(Priority.Medium);
    }
}
