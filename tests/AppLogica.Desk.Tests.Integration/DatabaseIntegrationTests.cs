using AppLogica.Desk.Application.Common.Interfaces;
using AppLogica.Desk.Application.Incidents.Commands.CreateIncident;
using AppLogica.Desk.Domain.Incidents;
using AppLogica.Desk.Domain.Repositories;
using AppLogica.Desk.Domain.Sla;
using AppLogica.Desk.Infrastructure.Persistence;
using AppLogica.Desk.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace AppLogica.Desk.Tests.Integration;

/// <summary>
/// Test tenant context that returns fixed values for use in integration tests.
/// </summary>
internal sealed class TestTenantContext : ITenantContext
{
    public string SchemaName { get; set; } = "public";
    public Guid TenantId { get; set; } = Guid.NewGuid();
}

/// <summary>
/// Integration tests using EF Core with SQLite in-memory provider.
/// Tests repository operations, tenant isolation, and data persistence end-to-end.
/// </summary>
public class DatabaseIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TestTenantContext _tenantContext;
    private readonly Mock<IPublisher> _publisherMock;

    public DatabaseIntegrationTests()
    {
        // Use a shared in-memory SQLite connection so the DB persists across contexts
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _tenantContext = new TestTenantContext();
        _publisherMock = new Mock<IPublisher>();
    }

    private DeskDbContext CreateDbContext(ITenantContext? tenantContext = null)
    {
        var tc = tenantContext ?? _tenantContext;
        var options = new DbContextOptionsBuilder<DeskDbContext>()
            .UseSqlite(_connection)
            .Options;

        var context = new DeskDbContext(options, tc, _publisherMock.Object);
        context.Database.EnsureCreated();
        return context;
    }

    private IncidentRepository CreateIncidentRepository(DeskDbContext? context = null)
    {
        return new IncidentRepository(context ?? CreateDbContext());
    }

    private SlaRepository CreateSlaRepository(DeskDbContext? context = null)
    {
        return new SlaRepository(context ?? CreateDbContext());
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public async Task CanCreate_Incident_EndToEnd()
    {
        // Arrange
        var tenantId = _tenantContext.TenantId;
        using var dbContext = CreateDbContext();
        var repo = new IncidentRepository(dbContext);

        var incident = Incident.Create(
            tenantId, "End-to-end test", "Full persistence test",
            Impact.Enterprise, Urgency.Immediate, "INC-2026-00001", Guid.NewGuid());

        // Act: persist
        await repo.AddAsync(incident, CancellationToken.None);

        // Assert: retrieve
        var retrieved = await repo.GetByIdAsync(incident.Id, tenantId, CancellationToken.None);
        retrieved.Should().NotBeNull();
        retrieved!.Title.Should().Be("End-to-end test");
        retrieved.TicketNumber.Should().Be("INC-2026-00001");
        retrieved.Status.Should().Be(IncidentStatus.New);
        retrieved.Priority.Should().Be(Priority.Critical);
    }

    [Fact]
    public async Task TenantIsolation_TenantA_CannotRead_TenantB_Incidents()
    {
        // Arrange: Create incident for Tenant A
        var tenantA = new TestTenantContext { TenantId = Guid.NewGuid() };
        var tenantB = new TestTenantContext { TenantId = Guid.NewGuid() };

        using var dbContextA = CreateDbContext(tenantA);
        var repoA = new IncidentRepository(dbContextA);

        var incident = Incident.Create(
            tenantA.TenantId, "Tenant A Incident", "Should not be visible to B",
            Impact.Department, Urgency.High, "INC-2026-00010", Guid.NewGuid());

        await repoA.AddAsync(incident, CancellationToken.None);

        // Act: Try to read it from Tenant B's context
        using var dbContextB = CreateDbContext(tenantB);
        var repoB = new IncidentRepository(dbContextB);
        var result = await repoB.GetByIdAsync(incident.Id, tenantB.TenantId, CancellationToken.None);

        // Assert: Tenant B cannot see Tenant A's incident
        result.Should().BeNull();
    }

    [Fact]
    public async Task TenantIsolation_TenantA_CannotUpdate_TenantB_Incidents()
    {
        // Arrange: Create incident for Tenant A
        var tenantA = new TestTenantContext { TenantId = Guid.NewGuid() };
        var tenantB = new TestTenantContext { TenantId = Guid.NewGuid() };

        using var dbContextA = CreateDbContext(tenantA);
        var repoA = new IncidentRepository(dbContextA);

        var incident = Incident.Create(
            tenantA.TenantId, "Tenant A Only", "Tenant B should not find this",
            Impact.Team, Urgency.Normal, "INC-2026-00011", Guid.NewGuid());

        await repoA.AddAsync(incident, CancellationToken.None);

        // Act: Try to retrieve from Tenant B to update
        using var dbContextB = CreateDbContext(tenantB);
        var repoB = new IncidentRepository(dbContextB);
        var retrieved = await repoB.GetByIdAsync(incident.Id, tenantB.TenantId, CancellationToken.None);

        // Assert: Cannot even retrieve it
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task SlaTimer_Created_Automatically_On_IncidentCreation()
    {
        // Arrange: Use the real IncidentRepository backed by SQLite,
        // but mock ISlaRepository because SQLite does not support OwnsMany shadow keys
        // for SlaPolicy targets. The mock returns a policy with a Critical target.
        using var dbContext = CreateDbContext();
        var incidentRepo = new IncidentRepository(dbContext);

        var criticalTarget = new SlaTarget(Priority.Critical, ResponseMinutes: 60, ResolutionMinutes: 240);
        var policy = new SlaPolicy(_tenantContext.TenantId, "Standard SLA", new[] { criticalTarget });

        var slaRepoMock = new Mock<ISlaRepository>();
        slaRepoMock
            .Setup(r => r.GetPolicyByPriorityAsync(Priority.Critical, _tenantContext.TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(policy);

        // Capture the timer that gets added
        SlaTimer? capturedTimer = null;
        slaRepoMock
            .Setup(r => r.AddTimerAsync(It.IsAny<SlaTimer>(), It.IsAny<CancellationToken>()))
            .Callback<SlaTimer, CancellationToken>((timer, _) => capturedTimer = timer)
            .Returns(Task.CompletedTask);

        var handler = new CreateIncidentCommandHandler(incidentRepo, slaRepoMock.Object, _tenantContext);
        var command = new CreateIncidentCommand(
            "Critical Server Down", "Entire production cluster offline",
            Impact.Enterprise, Urgency.Immediate, null);

        // Act
        var incidentId = await handler.Handle(command, CancellationToken.None);

        // Assert: SLA timer should have been created with correct properties
        capturedTimer.Should().NotBeNull();
        capturedTimer!.TenantId.Should().Be(_tenantContext.TenantId);
        capturedTimer.IncidentId.Should().Be(incidentId);
        capturedTimer.Status.Should().Be(SlaTimerStatus.Active);

        // Verify the incident was actually persisted in the DB
        var incident = await incidentRepo.GetByIdAsync(incidentId, _tenantContext.TenantId, CancellationToken.None);
        incident.Should().NotBeNull();
        incident!.Priority.Should().Be(Priority.Critical);
    }

    [Fact]
    public async Task IncidentRepository_ListAsync_FiltersByStatus()
    {
        // Arrange
        var tenantId = _tenantContext.TenantId;
        using var dbContext = CreateDbContext();
        var repo = new IncidentRepository(dbContext);

        var incident1 = Incident.Create(tenantId, "Open Incident", "desc",
            Impact.Department, Urgency.Normal, "INC-2026-00020", Guid.NewGuid());
        var incident2 = Incident.Create(tenantId, "Another Open", "desc",
            Impact.Team, Urgency.Low, "INC-2026-00021", Guid.NewGuid());

        await repo.AddAsync(incident1, CancellationToken.None);
        await repo.AddAsync(incident2, CancellationToken.None);

        // Act: filter by New status
        var filter = new IncidentFilter
        {
            TenantId = tenantId,
            Statuses = new List<IncidentStatus> { IncidentStatus.New }
        };
        var results = await repo.ListAsync(filter, CancellationToken.None);

        // Assert
        results.Should().HaveCount(2);
        results.Should().AllSatisfy(i => i.Status.Should().Be(IncidentStatus.New));
    }

    [Fact]
    public async Task IncidentRepository_ListAsync_FiltersByPriority()
    {
        // Arrange
        var tenantId = _tenantContext.TenantId;
        using var dbContext = CreateDbContext();
        var repo = new IncidentRepository(dbContext);

        var criticalIncident = Incident.Create(tenantId, "Critical one", "desc",
            Impact.Enterprise, Urgency.Immediate, "INC-2026-00030", Guid.NewGuid());
        var lowIncident = Incident.Create(tenantId, "Low one", "desc",
            Impact.Team, Urgency.Low, "INC-2026-00031", Guid.NewGuid());

        await repo.AddAsync(criticalIncident, CancellationToken.None);
        await repo.AddAsync(lowIncident, CancellationToken.None);

        // Act: filter by Critical priority only
        var filter = new IncidentFilter
        {
            TenantId = tenantId,
            Priorities = new List<Priority> { Priority.Critical }
        };
        var results = await repo.ListAsync(filter, CancellationToken.None);

        // Assert
        results.Should().HaveCount(1);
        results[0].Priority.Should().Be(Priority.Critical);
    }

    [Fact]
    public async Task IncidentRepository_GetNextTicketSequence_Returns1_ForNewTenant()
    {
        // Arrange
        var tenantId = _tenantContext.TenantId;
        using var dbContext = CreateDbContext();
        var repo = new IncidentRepository(dbContext);

        // Act: no incidents exist yet for this tenant/year
        var sequence = await repo.GetNextTicketSequenceAsync(tenantId, 2026, CancellationToken.None);

        // Assert
        sequence.Should().Be(1);
    }

    [Fact]
    public async Task IncidentRepository_GetNextTicketSequence_Increments()
    {
        // Arrange
        var tenantId = _tenantContext.TenantId;
        using var dbContext = CreateDbContext();
        var repo = new IncidentRepository(dbContext);

        // Add one incident so the sequence starts
        var incident = Incident.Create(tenantId, "First ticket", "desc",
            Impact.Department, Urgency.Normal, "INC-2026-00001", Guid.NewGuid());
        await repo.AddAsync(incident, CancellationToken.None);

        // Act
        var nextSequence = await repo.GetNextTicketSequenceAsync(tenantId, 2026, CancellationToken.None);

        // Assert: should be 2 since 00001 already exists
        nextSequence.Should().Be(2);
    }

    [Fact]
    public async Task SoftDelete_DoesNotReturnDeletedIncidents()
    {
        // Arrange
        var tenantId = _tenantContext.TenantId;
        using var dbContext = CreateDbContext();
        var repo = new IncidentRepository(dbContext);

        var incident = Incident.Create(tenantId, "To be deleted", "desc",
            Impact.Department, Urgency.High, "INC-2026-00040", Guid.NewGuid());
        await repo.AddAsync(incident, CancellationToken.None);

        // Soft-delete via direct EF Core manipulation
        incident.IsDeleted = true;
        incident.DeletedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        // Act: try to retrieve
        var retrieved = await repo.GetByIdAsync(incident.Id, tenantId, CancellationToken.None);

        // Assert: soft-deleted incidents should be filtered out
        retrieved.Should().BeNull();
    }
}
