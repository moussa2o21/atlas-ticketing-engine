using AppLogica.Desk.Application.Incidents.Commands.AssignIncident;
using AppLogica.Desk.Application.Incidents.Commands.CloseIncident;
using AppLogica.Desk.Application.Incidents.Commands.CreateIncident;
using AppLogica.Desk.Application.Incidents.Commands.EscalateIncident;
using AppLogica.Desk.Application.Incidents.Commands.ResolveIncident;
using AppLogica.Desk.Application.Incidents.DTOs;
using AppLogica.Desk.Application.Incidents.Queries.GetIncident;
using AppLogica.Desk.Application.Incidents.Queries.GetIncidentTimeline;
using AppLogica.Desk.Application.Incidents.Queries.ListIncidents;
using AppLogica.Desk.Domain.Incidents;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppLogica.Desk.API.Controllers;

/// <summary>
/// ITIL Incident lifecycle controller. All endpoints require authentication.
/// TenantId is resolved from the JWT by <see cref="Middleware.TenantResolutionMiddleware"/>
/// and consumed by MediatR handlers via <c>ITenantContext</c> — never from the route or body.
/// </summary>
[ApiController]
[Route("api/desk/incidents")]
[Authorize]
public sealed class IncidentsController : ControllerBase
{
    private readonly ISender _sender;

    public IncidentsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Create a new incident.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateIncident(
        [FromBody] CreateIncidentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new CreateIncidentCommand(
                request.Title,
                request.Description,
                request.Impact,
                request.Urgency,
                request.QueueId);

            var incidentId = await _sender.Send(command, cancellationToken);

            return CreatedAtAction(
                nameof(GetIncident),
                new { id = incidentId },
                new { id = incidentId });
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
    /// List incidents with filtering and pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<IncidentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListIncidents(
        [FromQuery(Name = "statuses")] List<IncidentStatus>? statuses,
        [FromQuery(Name = "priorities")] List<Priority>? priorities,
        [FromQuery] Guid? assigneeId,
        [FromQuery] Guid? queueId,
        [FromQuery] DateTime? createdFrom,
        [FromQuery] DateTime? createdTo,
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new ListIncidentsQuery(
                statuses,
                priorities,
                assigneeId,
                queueId,
                createdFrom,
                createdTo,
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
    /// Get a single incident by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(IncidentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetIncident(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetIncidentQuery(id);
            var result = await _sender.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFoundProblem(ex);
        }
    }

    /// <summary>
    /// Update mutable fields on an incident (not yet implemented).
    /// </summary>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IActionResult UpdateIncident(Guid id)
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc9110#section-15.6.2",
            Title = "Not Implemented",
            Status = StatusCodes.Status501NotImplemented,
            Detail = "PATCH update for incidents is not yet implemented.",
            Instance = HttpContext.Request.Path
        });
    }

    /// <summary>
    /// Assign an incident to an agent.
    /// </summary>
    [HttpPost("{id:guid}/assign")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignIncident(
        Guid id,
        [FromBody] AssignIncidentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new AssignIncidentCommand(id, request.AssigneeId);
            await _sender.Send(command, cancellationToken);
            return Ok(new { id, assigneeId = request.AssigneeId });
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
    /// Escalate an incident.
    /// </summary>
    [HttpPost("{id:guid}/escalate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> EscalateIncident(
        Guid id,
        [FromBody] EscalateIncidentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new EscalateIncidentCommand(id, request.Reason);
            await _sender.Send(command, cancellationToken);
            return Ok(new { id, escalated = true });
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
    /// Resolve an incident with resolution notes.
    /// </summary>
    [HttpPost("{id:guid}/resolve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ResolveIncident(
        Guid id,
        [FromBody] ResolveIncidentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new ResolveIncidentCommand(id, request.ResolutionNotes);
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
    /// Close a resolved incident.
    /// </summary>
    [HttpPost("{id:guid}/close")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CloseIncident(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new CloseIncidentCommand(id);
            await _sender.Send(command, cancellationToken);
            return Ok(new { id, closed = true });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFoundProblem(ex);
        }
        catch (InvalidOperationException ex)
        {
            return ConflictProblem(ex);
        }
    }

    /// <summary>
    /// Get the activity timeline for an incident.
    /// </summary>
    [HttpGet("{id:guid}/timeline")]
    [ProducesResponseType(typeof(IReadOnlyList<TimelineEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetIncidentTimeline(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetIncidentTimelineQuery(id);
            var result = await _sender.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFoundProblem(ex);
        }
    }

    /// <summary>
    /// Bulk assign incidents to an agent (not yet implemented).
    /// </summary>
    [HttpPost("bulk-assign")]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IActionResult BulkAssign()
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc9110#section-15.6.2",
            Title = "Not Implemented",
            Status = StatusCodes.Status501NotImplemented,
            Detail = "Bulk assign is not yet implemented.",
            Instance = HttpContext.Request.Path
        });
    }

    // ────────────────────────────── Request DTOs ──────────────────────────────

    /// <summary>Request body for creating an incident.</summary>
    public sealed record CreateIncidentRequest(
        string Title,
        string Description,
        Impact Impact,
        Urgency Urgency,
        Guid? QueueId);

    /// <summary>Request body for assigning an incident.</summary>
    public sealed record AssignIncidentRequest(Guid AssigneeId);

    /// <summary>Request body for escalating an incident.</summary>
    public sealed record EscalateIncidentRequest(string Reason);

    /// <summary>Request body for resolving an incident.</summary>
    public sealed record ResolveIncidentRequest(string ResolutionNotes);

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
