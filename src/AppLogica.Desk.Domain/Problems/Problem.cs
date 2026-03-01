using AppLogica.Desk.Domain.Common;
using AppLogica.Desk.Domain.Incidents;
using AppLogica.Desk.Domain.Problems.Events;

namespace AppLogica.Desk.Domain.Problems;

/// <summary>
/// Aggregate root representing an ITIL problem record. Enforces lifecycle state
/// transitions and raises domain events on key transitions.
/// </summary>
public sealed class Problem : AggregateRoot
{
    // ───────────────────────── Properties ─────────────────────────

    public string ProblemNumber { get; private set; } = default!;
    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }
    public ProblemStatus Status { get; private set; }
    public Priority Priority { get; private set; }
    public Impact Impact { get; private set; }
    public Guid? AssigneeId { get; private set; }
    public string? RootCause { get; private set; }
    public string? Workaround { get; private set; }
    public bool IsKnownError { get; private set; }
    public DateTime? KnownErrorPublishedAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }

    // Stored as JSONB array
    private readonly List<Guid> _linkedIncidentIds = [];
    public IReadOnlyList<Guid> LinkedIncidentIds => _linkedIncidentIds.AsReadOnly();

    // EF Core requires a parameterless constructor
    private Problem() { }

    // ───────────────────────── Factory ─────────────────────────

    /// <summary>
    /// Creates a new problem record and raises a <see cref="ProblemCreatedEvent"/>.
    /// </summary>
    public static Problem Create(
        Guid tenantId,
        string title,
        string? description,
        Priority priority,
        Impact impact,
        string problemNumber,
        Guid createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        ArgumentException.ThrowIfNullOrWhiteSpace(problemNumber, nameof(problemNumber));

        if (!problemNumber.StartsWith("PRB-") || problemNumber.Length != 14)
        {
            throw new ArgumentException(
                "Problem number must follow the format PRB-YYYY-NNNNN.",
                nameof(problemNumber));
        }

        var problem = new Problem
        {
            TenantId = tenantId,
            Title = title,
            Description = description,
            Priority = priority,
            Impact = impact,
            ProblemNumber = problemNumber,
            Status = ProblemStatus.New,
            IsKnownError = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        problem.AddDomainEvent(new ProblemCreatedEvent(
            problem.Id,
            problem.TenantId,
            problem.ProblemNumber,
            problem.Priority));

        return problem;
    }

    // ───────────────────────── State Transitions ─────────────────────────

    /// <summary>
    /// Opens the problem for investigation. Transitions: New -> Open.
    /// </summary>
    public void Open(Guid openedBy)
    {
        GuardStatus(ProblemStatus.New, "open");

        Status = ProblemStatus.Open;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = openedBy;
    }

    /// <summary>
    /// Begins investigation of the problem. Transitions: Open -> Investigating.
    /// </summary>
    public void Investigate(Guid investigatorId)
    {
        GuardStatus(ProblemStatus.Open, "investigate");

        AssigneeId = investigatorId;
        Status = ProblemStatus.Investigating;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = investigatorId;
    }

    /// <summary>
    /// Records the root cause analysis. Transitions: Investigating -> RootCauseIdentified.
    /// </summary>
    public void IdentifyRootCause(string rootCause, Guid identifiedBy)
    {
        GuardStatus(ProblemStatus.Investigating, "identify root cause for");
        ArgumentException.ThrowIfNullOrWhiteSpace(rootCause, nameof(rootCause));

        RootCause = rootCause;
        Status = ProblemStatus.RootCauseIdentified;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = identifiedBy;
    }

    /// <summary>
    /// Resolves the problem with a resolution and optional workaround.
    /// Transitions: RootCauseIdentified -> Resolved, KnownError -> Resolved.
    /// </summary>
    public void Resolve(string rootCause, string? workaround, Guid resolvedBy)
    {
        if (Status is not (ProblemStatus.RootCauseIdentified or ProblemStatus.KnownError))
        {
            throw new InvalidOperationException(
                $"Cannot resolve problem in '{Status}' status. " +
                "Problem must be in 'RootCauseIdentified' or 'KnownError' status.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(rootCause, nameof(rootCause));

        RootCause = rootCause;
        Workaround = workaround;
        ResolvedAt = DateTime.UtcNow;
        Status = ProblemStatus.Resolved;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = resolvedBy;

        AddDomainEvent(new ProblemResolvedEvent(Id, TenantId, ProblemNumber));
    }

    /// <summary>
    /// Closes a resolved problem. Transitions: Resolved -> Closed.
    /// </summary>
    public void Close(Guid closedBy)
    {
        GuardStatus(ProblemStatus.Resolved, "close");

        ClosedAt = DateTime.UtcNow;
        Status = ProblemStatus.Closed;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = closedBy;
    }

    // ───────────────────────── Known Error Management ─────────────────────────

    /// <summary>
    /// Publishes the problem as a Known Error to the KEDB.
    /// Transitions: RootCauseIdentified -> KnownError.
    /// </summary>
    public void PublishAsKnownError(string workaround, Guid publishedBy)
    {
        GuardStatus(ProblemStatus.RootCauseIdentified, "publish as known error");
        ArgumentException.ThrowIfNullOrWhiteSpace(workaround, nameof(workaround));

        Workaround = workaround;
        IsKnownError = true;
        KnownErrorPublishedAt = DateTime.UtcNow;
        Status = ProblemStatus.KnownError;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = publishedBy;
    }

    // ───────────────────────── Incident Linking ─────────────────────────

    /// <summary>
    /// Links an incident to this problem.
    /// </summary>
    public void LinkIncident(Guid incidentId)
    {
        if (_linkedIncidentIds.Contains(incidentId))
        {
            return; // Idempotent - already linked
        }

        _linkedIncidentIds.Add(incidentId);
        UpdatedAt = DateTime.UtcNow;
    }

    // ───────────────────────── Mutators ─────────────────────────

    /// <summary>
    /// Updates mutable fields on the problem.
    /// </summary>
    public void Update(
        string? title,
        string? description,
        Priority? priority,
        Impact? impact,
        Guid? assigneeId,
        Guid updatedBy)
    {
        if (title is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
            Title = title;
        }

        if (description is not null)
        {
            Description = description;
        }

        if (priority.HasValue)
        {
            Priority = priority.Value;
        }

        if (impact.HasValue)
        {
            Impact = impact.Value;
        }

        if (assigneeId.HasValue)
        {
            AssigneeId = assigneeId.Value;
        }

        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    // ───────────────────────── Guard Methods ─────────────────────────

    private void GuardStatus(ProblemStatus expected, string action)
    {
        if (Status != expected)
        {
            throw new InvalidOperationException(
                $"Cannot {action} problem in '{Status}' status. " +
                $"Problem must be in '{expected}' status.");
        }
    }
}
