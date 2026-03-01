using AppLogica.Desk.Domain.Common;

namespace AppLogica.Desk.Domain.Sla;

/// <summary>
/// An SLA policy defining response and resolution targets for each priority level.
/// Each tenant can have its own SLA policies.
/// </summary>
public sealed class SlaPolicy : Entity
{
    private readonly List<SlaTarget> _targets = [];

    /// <summary>
    /// Human-readable name for this SLA policy (e.g. "Standard SLA", "Premium SLA").
    /// </summary>
    public string Name { get; private set; } = default!;

    /// <summary>
    /// The SLA targets (one per priority level) associated with this policy.
    /// </summary>
    public IReadOnlyList<SlaTarget> Targets => _targets.AsReadOnly();

    // EF Core requires a parameterless constructor
    private SlaPolicy() { }

    /// <summary>
    /// Creates a new SLA policy with the given name and targets.
    /// </summary>
    public SlaPolicy(Guid tenantId, string name, IEnumerable<SlaTarget> targets)
    {
        TenantId = tenantId;
        Name = name;
        CreatedAt = DateTime.UtcNow;
        _targets.AddRange(targets);
    }
}
