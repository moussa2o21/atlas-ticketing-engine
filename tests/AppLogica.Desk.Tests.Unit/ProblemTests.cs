using AppLogica.Desk.Domain.Incidents;
using AppLogica.Desk.Domain.Problems;
using AppLogica.Desk.Domain.Problems.Events;
using FluentAssertions;

namespace AppLogica.Desk.Tests.Unit;

public class ProblemTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _createdBy = Guid.NewGuid();

    private Problem CreateTestProblem(
        Priority priority = Priority.Medium,
        Impact impact = Impact.Department)
    {
        return Problem.Create(
            _tenantId, "Test Problem", "Test description",
            priority, impact, "PRB-2026-00001", _createdBy);
    }

    // ───────────────────── 1. Problem lifecycle ─────────────────────

    [Fact]
    public void Problem_Lifecycle_FullTransition_NewToClose()
    {
        var problem = CreateTestProblem();
        var userId = Guid.NewGuid();

        // New -> Open
        problem.Open(userId);
        problem.Status.Should().Be(ProblemStatus.Open);

        // Open -> Investigating
        problem.Investigate(userId);
        problem.Status.Should().Be(ProblemStatus.Investigating);
        problem.AssigneeId.Should().Be(userId);

        // Investigating -> RootCauseIdentified
        problem.IdentifyRootCause("{\"category\":\"Software\"}", userId);
        problem.Status.Should().Be(ProblemStatus.RootCauseIdentified);
        problem.RootCause.Should().NotBeNullOrEmpty();

        // RootCauseIdentified -> Resolved
        problem.Resolve("{\"category\":\"Software\",\"fix\":\"patched\"}", "Apply patch v2.1", userId);
        problem.Status.Should().Be(ProblemStatus.Resolved);
        problem.ResolvedAt.Should().NotBeNull();
        problem.Workaround.Should().Be("Apply patch v2.1");

        // Resolved -> Closed
        problem.Close(userId);
        problem.Status.Should().Be(ProblemStatus.Closed);
        problem.ClosedAt.Should().NotBeNull();
    }

    // ───────────────────── 2. State machine guards ─────────────────────

    [Fact]
    public void StateMachine_Guards_ThrowOnInvalidTransitions()
    {
        var problem = CreateTestProblem();
        var userId = Guid.NewGuid();

        // Cannot investigate from New (must be Open first)
        var actInvestigate = () => problem.Investigate(userId);
        actInvestigate.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot investigate*");

        // Cannot resolve from New
        var actResolve = () => problem.Resolve("root cause", null, userId);
        actResolve.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot resolve*");

        // Cannot close from New
        var actClose = () => problem.Close(userId);
        actClose.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot close*");

        // Cannot publish as known error from New
        var actPublish = () => problem.PublishAsKnownError("workaround", userId);
        actPublish.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot publish*");

        // Transition to Open, then try invalid transitions
        problem.Open(userId);

        // Cannot open again
        var actOpenAgain = () => problem.Open(userId);
        actOpenAgain.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot open*");

        // Cannot identify root cause from Open (must be Investigating first)
        var actIdentify = () => problem.IdentifyRootCause("{}", userId);
        actIdentify.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot identify root cause*");
    }

    // ───────────────────── 3. Link incident to problem ─────────────────────

    [Fact]
    public void LinkIncident_AddsIncidentId_And_IsIdempotent()
    {
        var problem = CreateTestProblem();
        var incidentId1 = Guid.NewGuid();
        var incidentId2 = Guid.NewGuid();

        problem.LinkIncident(incidentId1);
        problem.LinkedIncidentIds.Should().ContainSingle()
            .Which.Should().Be(incidentId1);

        // Add another
        problem.LinkIncident(incidentId2);
        problem.LinkedIncidentIds.Should().HaveCount(2);

        // Idempotent: linking same incident again should not duplicate
        problem.LinkIncident(incidentId1);
        problem.LinkedIncidentIds.Should().HaveCount(2);
    }

    // ───────────────────── 4. Known error publishing ─────────────────────

    [Fact]
    public void PublishAsKnownError_TransitionsCorrectly()
    {
        var problem = CreateTestProblem();
        var userId = Guid.NewGuid();

        // Progress to RootCauseIdentified
        problem.Open(userId);
        problem.Investigate(userId);
        problem.IdentifyRootCause("{\"category\":\"Hardware\"}", userId);

        // Publish as Known Error
        problem.PublishAsKnownError("Replace faulty component", userId);

        problem.Status.Should().Be(ProblemStatus.KnownError);
        problem.IsKnownError.Should().BeTrue();
        problem.KnownErrorPublishedAt.Should().NotBeNull();
        problem.Workaround.Should().Be("Replace faulty component");

        // Known Error can still be resolved
        problem.Resolve("{\"category\":\"Hardware\",\"fix\":\"replaced\"}", "Permanent fix applied", userId);
        problem.Status.Should().Be(ProblemStatus.Resolved);
    }

    // ───────────────────── 5. Problem number format ─────────────────────

    [Fact]
    public void ProblemNumber_MustFollowFormat_PRB_YYYY_NNNNN()
    {
        // Valid format
        var problem = CreateTestProblem();
        problem.ProblemNumber.Should().Be("PRB-2026-00001");
        problem.ProblemNumber.Should().StartWith("PRB-");
        problem.ProblemNumber.Should().HaveLength(14);

        // Invalid format: wrong prefix
        var actInvalidPrefix = () => Problem.Create(
            _tenantId, "Title", null, Priority.Low, Impact.Individual,
            "INC-2026-00001", _createdBy);
        actInvalidPrefix.Should().Throw<ArgumentException>()
            .WithMessage("*PRB-YYYY-NNNNN*");

        // Invalid format: wrong length
        var actInvalidLength = () => Problem.Create(
            _tenantId, "Title", null, Priority.Low, Impact.Individual,
            "PRB-2026-1", _createdBy);
        actInvalidLength.Should().Throw<ArgumentException>()
            .WithMessage("*PRB-YYYY-NNNNN*");
    }

    // ───────────────────── 6. RootCauseEntry structure ─────────────────────

    [Fact]
    public void RootCauseEntry_ValueObject_EqualityAndProperties()
    {
        var now = DateTime.UtcNow;
        var identifiedBy = Guid.NewGuid();

        var entry1 = new RootCauseEntry("Software", "Null reference in module X", now, identifiedBy);
        var entry2 = new RootCauseEntry("Software", "Null reference in module X", now, identifiedBy);
        var entry3 = new RootCauseEntry("Hardware", "Disk failure", now, identifiedBy);

        // Record value equality
        entry1.Should().Be(entry2);
        entry1.Should().NotBe(entry3);

        // Property access
        entry1.Category.Should().Be("Software");
        entry1.Description.Should().Be("Null reference in module X");
        entry1.IdentifiedAt.Should().Be(now);
        entry1.IdentifiedBy.Should().Be(identifiedBy);
    }

    // ───────────────────── Additional: Domain Events ─────────────────────

    [Fact]
    public void DomainEvent_ProblemCreatedEvent_RaisedOnCreate()
    {
        var problem = CreateTestProblem();

        problem.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProblemCreatedEvent>()
            .Which.ProblemNumber.Should().Be("PRB-2026-00001");
    }

    [Fact]
    public void DomainEvent_ProblemResolvedEvent_RaisedOnResolve()
    {
        var problem = CreateTestProblem();
        var userId = Guid.NewGuid();

        problem.Open(userId);
        problem.Investigate(userId);
        problem.IdentifyRootCause("{}", userId);
        problem.ClearDomainEvents();

        problem.Resolve("{\"fix\":\"applied\"}", null, userId);

        problem.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProblemResolvedEvent>()
            .Which.ProblemNumber.Should().Be("PRB-2026-00001");
    }
}
