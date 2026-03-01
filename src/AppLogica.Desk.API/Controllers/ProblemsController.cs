using AppLogica.Desk.Application.Incidents.DTOs;
using AppLogica.Desk.Application.Problems;
using AppLogica.Desk.Application.Problems.Commands.CreateProblem;
using AppLogica.Desk.Application.Problems.Commands.LinkIncidentToProblem;
using AppLogica.Desk.Application.Problems.Commands.PublishKnownError;
using AppLogica.Desk.Application.Problems.Commands.ResolveProblem;
using AppLogica.Desk.Application.Problems.DTOs;
using AppLogica.Desk.Application.Problems.Queries.GetKnownErrors;
using AppLogica.Desk.Application.Problems.Queries.GetProblem;
using AppLogica.Desk.Application.Problems.Queries.ListProblems;
using AppLogica.Desk.Domain.Incidents;
using AppLogica.Desk.Domain.Problems;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppLogica.Desk.API.Controllers;

/// <summary>
/// ITIL Problem Management controller. All endpoints require authentication.
/// TenantId is resolved from the JWT by <see cref="Middleware.TenantResolutionMiddleware"/>
/// and consumed by MediatR handlers via <c>ITenantContext</c> — never from the route or body.
/// </summary>
[ApiController]
[Route("api/desk/problems")]
[Authorize]
public sealed class ProblemsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IAiTrendDetectionService _aiTrendDetectionService;

    public ProblemsController(ISender sender, IAiTrendDetectionService aiTrendDetectionService)
    {
        _sender = sender;
        _aiTrendDetectionService = aiTrendDetectionService;
    }

    /// <summary>
    /// Create a new problem.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateProblem(
        [FromBody] CreateProblemRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new CreateProblemCommand(
                request.Title,
                request.Description,
                request.Priority,
                request.Impact);

            var problemId = await _sender.Send(command, cancellationToken);

            return CreatedAtAction(
                nameof(GetProblem),
                new { id = problemId },
                new { id = problemId });
        }
        catch (FluentValidation.ValidationException ex)
        {
            return ValidationProblem(ex);
        }
        catch (InvalidOperationException ex)
        {
            return ConflictProblem(ex);
        }
    }

    /// <summary>
    /// List problems with filtering and pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProblemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListProblems(
        [FromQuery(Name = "statuses")] List<ProblemStatus>? statuses,
        [FromQuery] Priority? priority,
        [FromQuery] Guid? assigneeId,
        [FromQuery] bool? isKnownError,
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new ListProblemsQuery(
                statuses,
                priority,
                assigneeId,
                isKnownError,
                q,
                page,
                pageSize);

            var result = await _sender.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (FluentValidation.ValidationException ex)
        {
            return ValidationProblem(ex);
        }
    }

    /// <summary>
    /// Get a single problem by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProblemDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProblem(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetProblemQuery(id);
            var result = await _sender.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFoundProblem(ex);
        }
    }

    /// <summary>
    /// Update mutable fields on a problem (not yet implemented).
    /// </summary>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IActionResult UpdateProblem(Guid id)
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc9110#section-15.6.2",
            Title = "Not Implemented",
            Status = StatusCodes.Status501NotImplemented,
            Detail = "PATCH update for problems is not yet implemented.",
            Instance = HttpContext.Request.Path
        });
    }

    /// <summary>
    /// Link an incident to a problem.
    /// </summary>
    [HttpPost("{id:guid}/link-incident")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> LinkIncident(
        Guid id,
        [FromBody] LinkIncidentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new LinkIncidentToProblemCommand(id, request.IncidentId);
            await _sender.Send(command, cancellationToken);
            return Ok(new { problemId = id, incidentId = request.IncidentId, linked = true });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFoundProblem(ex);
        }
        catch (InvalidOperationException ex)
        {
            return ConflictProblem(ex);
        }
        catch (FluentValidation.ValidationException ex)
        {
            return ValidationProblem(ex);
        }
    }

    /// <summary>
    /// Resolve a problem with root cause.
    /// </summary>
    [HttpPost("{id:guid}/resolve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ResolveProblem(
        Guid id,
        [FromBody] ResolveProblemRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new ResolveProblemCommand(id, request.RootCause, request.Workaround);
            await _sender.Send(command, cancellationToken);
            return Ok(new { id, resolved = true });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFoundProblem(ex);
        }
        catch (InvalidOperationException ex)
        {
            return ConflictProblem(ex);
        }
        catch (FluentValidation.ValidationException ex)
        {
            return ValidationProblem(ex);
        }
    }

    /// <summary>
    /// Publish a problem as a Known Error to the KEDB.
    /// </summary>
    [HttpPost("{id:guid}/publish-known-error")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PublishKnownError(
        Guid id,
        [FromBody] PublishKnownErrorRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new PublishKnownErrorCommand(id, request.Workaround);
            await _sender.Send(command, cancellationToken);
            return Ok(new { id, publishedAsKnownError = true });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFoundProblem(ex);
        }
        catch (InvalidOperationException ex)
        {
            return ConflictProblem(ex);
        }
        catch (FluentValidation.ValidationException ex)
        {
            return ValidationProblem(ex);
        }
    }

    /// <summary>
    /// List all Known Errors from the KEDB.
    /// </summary>
    [HttpGet("/api/desk/known-errors")]
    [ProducesResponseType(typeof(IReadOnlyList<KnownErrorDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetKnownErrors(
        CancellationToken cancellationToken)
    {
        var query = new GetKnownErrorsQuery();
        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Trigger AI trend detection to suggest potential problems from incident patterns (stub).
    /// </summary>
    [HttpPost("detect-trends")]
    [ProducesResponseType(typeof(IReadOnlyList<TrendDetectionResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DetectTrends(
        CancellationToken cancellationToken)
    {
        // TenantId will be resolved by the service via ITenantContext
        var tenantId = Guid.Empty; // TODO: resolve from ITenantContext when injected here
        var results = await _aiTrendDetectionService.DetectTrendsAsync(tenantId, cancellationToken);
        return Ok(results);
    }

    // ────────────────────────────── Request DTOs ──────────────────────────────

    /// <summary>Request body for creating a problem.</summary>
    public sealed record CreateProblemRequest(
        string Title,
        string? Description,
        Priority Priority,
        Impact Impact);

    /// <summary>Request body for linking an incident to a problem.</summary>
    public sealed record LinkIncidentRequest(Guid IncidentId);

    /// <summary>Request body for resolving a problem.</summary>
    public sealed record ResolveProblemRequest(string RootCause, string? Workaround);

    /// <summary>Request body for publishing a known error.</summary>
    public sealed record PublishKnownErrorRequest(string Workaround);

    // ────────────────────────────── Error Helpers ─────────────────────────────

    private ObjectResult NotFoundProblem(KeyNotFoundException ex)
    {
        return new ObjectResult(new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
            Title = "Not Found",
            Status = StatusCodes.Status404NotFound,
            Detail = ex.Message,
            Instance = HttpContext.Request.Path
        })
        {
            StatusCode = StatusCodes.Status404NotFound
        };
    }

    private ObjectResult ConflictProblem(InvalidOperationException ex)
    {
        return new ObjectResult(new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.10",
            Title = "Conflict",
            Status = StatusCodes.Status409Conflict,
            Detail = ex.Message,
            Instance = HttpContext.Request.Path
        })
        {
            StatusCode = StatusCodes.Status409Conflict
        };
    }

    private ObjectResult ValidationProblem(FluentValidation.ValidationException ex)
    {
        var errors = ex.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        var problemDetails = new ValidationProblemDetails(errors)
        {
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            Title = "Validation Failed",
            Status = StatusCodes.Status400BadRequest,
            Detail = "One or more validation errors occurred.",
            Instance = HttpContext.Request.Path
        };

        return new ObjectResult(problemDetails)
        {
            StatusCode = StatusCodes.Status400BadRequest
        };
    }
}
