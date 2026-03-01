namespace AppLogica.Desk.Application.Problems;

/// <summary>
/// Service interface for AI-powered trend detection across incidents.
/// The actual implementation will connect to ATLAS AI Gateway in a future phase.
/// </summary>
public interface IAiTrendDetectionService
{
    /// <summary>
    /// Analyzes recent incidents to detect trends and suggest potential problems.
    /// </summary>
    /// <returns>A list of potential problem suggestions based on incident patterns.</returns>
    Task<IReadOnlyList<TrendDetectionResult>> DetectTrendsAsync(
        Guid tenantId,
        CancellationToken ct);
}

/// <summary>
/// Result of an AI trend detection analysis, representing a potential problem suggestion.
/// </summary>
public sealed record TrendDetectionResult(
    string SuggestedTitle,
    string SuggestedDescription,
    IReadOnlyList<Guid> RelatedIncidentIds,
    double ConfidenceScore);
