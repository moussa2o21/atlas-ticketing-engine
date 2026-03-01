using AppLogica.Desk.Application.Problems;
using Microsoft.Extensions.Logging;

namespace AppLogica.Desk.Infrastructure.Services.AiTrendDetection;

/// <summary>
/// Stub implementation of <see cref="IAiTrendDetectionService"/>.
/// Logs a message and returns an empty result. The actual implementation
/// will connect to ATLAS AI Gateway in a future phase.
/// </summary>
public sealed class StubAiTrendDetectionService : IAiTrendDetectionService
{
    private readonly ILogger<StubAiTrendDetectionService> _logger;

    public StubAiTrendDetectionService(ILogger<StubAiTrendDetectionService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<TrendDetectionResult>> DetectTrendsAsync(
        Guid tenantId,
        CancellationToken ct)
    {
        _logger.LogWarning(
            "AI trend detection not yet implemented. " +
            "This stub will be replaced with ATLAS AI Gateway integration in a future phase. " +
            "TenantId: {TenantId}", tenantId);

        IReadOnlyList<TrendDetectionResult> emptyResults = Array.Empty<TrendDetectionResult>();
        return Task.FromResult(emptyResults);
    }
}
