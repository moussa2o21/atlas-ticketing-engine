using AppLogica.Desk.Domain.Common;
using AppLogica.Desk.Domain.Incidents.Events;

namespace AppLogica.Desk.Domain.Incidents;

/// <summary>
/// Aggregate root representing an ITIL incident. Enforces lifecycle state
/// transitions and raises domain events on each transition.
/// </summary>
public sealed class Incident : AggregateRoot
{
    // ───────────────────────── Properties ─────────────────────────

    public string TicketNumber { get; private set; } = default!;
    public string Title { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public IncidentStatus Status { get; private set; }
    public Priority Priority { get; private set; }
    public Impact Impact { get; private set; }
    public Urgency Urgency { get; private set; }
    public IncidentType IncidentType { get; private set; }
    public Guid? AssigneeId { get; private set; }
    public Guid? QueueId { get; private set; }
    public string? ResolutionNotes { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public bool IsMajorIncident { get; private set; }

    // EF Core requires a parameterless constructor
    private Incident() { }

    // ───────────────────────── Factory ─────────────────────────

    /// <summary>
    /// Creates a new incident, calculates priority from the ITIL Impact x Urgency
    /// matrix, and raises an <see cref="IncidentCreatedEvent"/>.
    /// </summary>
    public static Incident Create(
        Guid tenantId,
        string title,
        string description,
        Impact impact,
        Urgency urgency,
        string ticketNumber,
        Guid createdBy)
    {
        var incident = new Incident
        {
            TenantId = tenantId,
            Title = title,
            Description = description,
            Impact = impact,
            Urgency = urgency,
            TicketNumber = ticketNumber,
            Status = IncidentStatus.New,
            Priority = CalculatePriority(impact, urgency),
            IncidentType = IncidentType.Incident,
            IsMajorIncident = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        incident.AddDomainEvent(new IncidentCreatedEvent(
            incident.Id,
            incident.TenantId,
            incident.TicketNumber,
            incident.Priority,
            incident.Impact,
            incident.Urgency));

        return incident;
    }

    // ───────────────────────── ITIL Priority Matrix ─────────────────────────

    /// <summary>
    /// Calculates priority using the standard ITIL Impact x Urgency matrix.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    ///   <item>Critical  = Enterprise + Immediate</item>
    ///   <item>High      = Enterprise + High  OR  Department + Immediate</item>
    ///   <item>Medium    = Department + High  OR  Team + Immediate  OR  Enterprise + Normal</item>
    ///   <item>Low       = everything else</item>
    /// </list>
    /// </remarks>
    private static Priority CalculatePriority(Impact impact, Urgency urgency)
    {
        return (impact, urgency) switch
        {
            (Impact.Enterprise, Urgency.Immediate) => Priority.Critical,

            (Impact.Enterprise, Urgency.High) => Priority.High,
            (Impact.Department, Urgency.Immediate) => Priority.High,

            (Impact.Department, Urgency.High) => Priority.Medium,
            (Impact.Team, Urgency.Immediate) => Priority.Medium,
            (Impact.Enterprise, Urgency.Normal) => Priority.Medium,

            _ => Priority.Low
        };
    }

    // ───────────────────────── State Transitions ─────────────────────────

    /// <summary>
    /// Assigns the incident to an agent. Transitions: New -> Assigned.
    /// </summary>
    public void Assign(Guid assigneeId, Guid assignedBy)
    {
        GuardCanAssign();

        AssigneeId = assigneeId;
        Status = IncidentStatus.Assigned;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = assignedBy;

        AddDomainEvent(new IncidentAssignedEvent(Id, TenantId, assigneeId));
    }

    /// <summary>
    /// Escalates the incident. Transitions: Assigned -> Escalated, InProgress -> Escalated.
    /// </summary>
    public void Escalate(string reason, Guid escalatedBy)
    {
        GuardCanEscalate();

        Status = IncidentStatus.Escalated;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = escalatedBy;

        AddDomainEvent(new IncidentEscalatedEvent(Id, TenantId, reason));
    }

    /// <summary>
    /// Resolves the incident with resolution notes.
    /// Transitions: InProgress -> Resolved, Escalated -> Resolved, Major -> Resolved.
    /// </summary>
    public void Resolve(string notes, Guid resolvedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(notes, nameof(notes));
        GuardCanResolve();

        ResolutionNotes = notes;
        ResolvedAt = DateTime.UtcNow;
        Status = IncidentStatus.Resolved;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = resolvedBy;

        AddDomainEvent(new IncidentResolvedEvent(Id, TenantId, notes));
    }

    /// <summary>
    /// Closes a resolved incident. Transitions: Resolved -> Closed.
    /// </summary>
    public void Close(Guid closedBy)
    {
        GuardCanClose();

        ClosedAt = DateTime.UtcNow;
        Status = IncidentStatus.Closed;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = closedBy;

        AddDomainEvent(new IncidentClosedEvent(Id, TenantId));
    }

    /// <summary>
    /// Declares this incident as a major incident. Transitions: InProgress -> Major, Escalated -> Major.
    /// </summary>
    public void DeclareMajorIncident(Guid declaredBy)
    {
        GuardCanDeclareMajor();

        IsMajorIncident = true;
        IncidentType = IncidentType.MajorIncident;
        Status = IncidentStatus.Major;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = declaredBy;
    }

    /// <summary>
    /// Moves the incident to InProgress. Transitions: Assigned -> InProgress,
    /// Pending -> InProgress, Escalated -> InProgress.
    /// </summary>
    public void StartProgress(Guid startedBy)
    {
        GuardCanStartProgress();

        Status = IncidentStatus.InProgress;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = startedBy;
    }

    /// <summary>
    /// Places the incident on hold pending external input. Transitions: InProgress -> Pending.
    /// </summary>
    public void Pend(Guid pendedBy)
    {
        GuardCanPend();

        Status = IncidentStatus.Pending;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = pendedBy;
    }

    // ───────────────────────── Guard Methods ─────────────────────────

    private void GuardCanAssign()
    {
        if (Status is not IncidentStatus.New)
        {
            throw new InvalidOperationException(
                $"Cannot assign incident in '{Status}' status. Incident must be in 'New' status.");
        }
    }

    private void GuardCanEscalate()
    {
        if (Status is not (IncidentStatus.Assigned or IncidentStatus.InProgress))
        {
            throw new InvalidOperationException(
                $"Cannot escalate incident in '{Status}' status. Incident must be in 'Assigned' or 'InProgress' status.");
        }
    }

    private void GuardCanResolve()
    {
        if (Status is not (IncidentStatus.InProgress or IncidentStatus.Escalated or IncidentStatus.Major))
        {
            throw new InvalidOperationException(
                $"Cannot resolve incident in '{Status}' status. Incident must be in 'InProgress', 'Escalated', or 'Major' status.");
        }
    }

    private void GuardCanClose()
    {
        if (Status is not IncidentStatus.Resolved)
        {
            throw new InvalidOperationException(
                $"Cannot close incident in '{Status}' status. Incident must be in 'Resolved' status.");
        }
    }

    private void GuardCanDeclareMajor()
    {
        if (Status is not (IncidentStatus.InProgress or IncidentStatus.Escalated))
        {
            throw new InvalidOperationException(
                $"Cannot declare major incident in '{Status}' status. Incident must be in 'InProgress' or 'Escalated' status.");
        }
    }

    private void GuardCanStartProgress()
    {
        if (Status is not (IncidentStatus.Assigned or IncidentStatus.Pending or IncidentStatus.Escalated))
        {
            throw new InvalidOperationException(
                $"Cannot start progress on incident in '{Status}' status. Incident must be in 'Assigned', 'Pending', or 'Escalated' status.");
        }
    }

    private void GuardCanPend()
    {
        if (Status is not IncidentStatus.InProgress)
        {
            throw new InvalidOperationException(
                $"Cannot pend incident in '{Status}' status. Incident must be in 'InProgress' status.");
        }
    }
}
