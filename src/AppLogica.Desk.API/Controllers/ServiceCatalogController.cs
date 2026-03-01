using AppLogica.Desk.Application.Incidents.DTOs;
using AppLogica.Desk.Application.ServiceCatalog.Commands.ApproveServiceRequest;
using AppLogica.Desk.Application.ServiceCatalog.Commands.CreateServiceRequest;
using AppLogica.Desk.Application.ServiceCatalog.Commands.FulfillServiceRequest;
using AppLogica.Desk.Application.ServiceCatalog.Commands.RejectServiceRequest;
using AppLogica.Desk.Application.ServiceCatalog.DTOs;
using AppLogica.Desk.Application.ServiceCatalog.Queries.GetServiceCatalog;
using AppLogica.Desk.Application.ServiceCatalog.Queries.GetServiceRequest;
using AppLogica.Desk.Application.ServiceCatalog.Queries.ListServiceRequests;
using AppLogica.Desk.Domain.ServiceCatalog;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppLogica.Desk.API.Controllers;

/// <summary>
/// Service Catalog and Request Fulfillment controller. All endpoints require authentication.
/// TenantId is resolved from the JWT by <see cref="Middleware.TenantResolutionMiddleware"/>
/// and consumed by MediatR handlers via <c>ITenantContext</c> — never from the route or body.
/// </summary>
[ApiController]
[Authorize]
public sealed class ServiceCatalogController : ControllerBase
{
    private readonly ISender _sender;

    public ServiceCatalogController(ISender sender)
    {
        _sender = sender;
    }

    // ────────────────────────────── Catalog Endpoints ──────────────────────────────

    /// <summary>
    /// List all active service catalog categories with their items.
    /// </summary>
    [HttpGet("api/desk/catalog/categories")]
    [ProducesResponseType(typeof(IReadOnlyList<ServiceCatalogCategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListCategories(CancellationToken cancellationToken)
    {
        var query = new GetServiceCatalogQuery();
        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// List all active service catalog items.
    /// </summary>
    [HttpGet("api/desk/catalog/items")]
    [ProducesResponseType(typeof(IReadOnlyList<ServiceCatalogCategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListItems(CancellationToken cancellationToken)
    {
        // Returns categories with their items — the full catalog
        var query = new GetServiceCatalogQuery();
        var result = await _sender.Send(query, cancellationToken);

        // Flatten to items only
        var items = result
            .SelectMany(c => c.Items)
            .ToList();

        return Ok(items);
    }

    /// <summary>
    /// Get a catalog item detail by ID.
    /// </summary>
    [HttpGet("api/desk/catalog/items/{id:guid}")]
    [ProducesResponseType(typeof(ServiceCatalogItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetItem(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetServiceCatalogQuery();
        var result = await _sender.Send(query, cancellationToken);

        var item = result
            .SelectMany(c => c.Items)
            .FirstOrDefault(i => i.Id == id);

        if (item is null)
        {
            return NotFoundProblem(new KeyNotFoundException($"Catalog item '{id}' not found."));
        }

        return Ok(item);
    }

    // ────────────────────────────── Request Endpoints ──────────────────────────────

    /// <summary>
    /// Submit a new service request.
    /// </summary>
    [HttpPost("api/desk/requests")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateServiceRequest(
        [FromBody] CreateServiceRequestApiRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new CreateServiceRequestCommand(
                request.Title,
                request.Description,
                request.CatalogItemId);

            var requestId = await _sender.Send(command, cancellationToken);

            return CreatedAtAction(
                nameof(GetServiceRequest),
                new { id = requestId },
                new { id = requestId });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFoundProblem(ex);
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
    /// List service requests with filtering and pagination.
    /// </summary>
    [HttpGet("api/desk/requests")]
    [ProducesResponseType(typeof(PagedResult<ServiceRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListServiceRequests(
        [FromQuery(Name = "statuses")] List<ServiceRequestStatus>? statuses,
        [FromQuery] Guid? requesterId,
        [FromQuery] Guid? catalogItemId,
        [FromQuery] Guid? assigneeId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new ListServiceRequestsQuery(
            statuses,
            requesterId,
            catalogItemId,
            assigneeId,
            page,
            pageSize);

        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get a single service request by ID.
    /// </summary>
    [HttpGet("api/desk/requests/{id:guid}")]
    [ProducesResponseType(typeof(ServiceRequestDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetServiceRequest(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetServiceRequestQuery(id);
            var result = await _sender.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFoundProblem(ex);
        }
    }

    /// <summary>
    /// Approve a service request that is pending approval.
    /// </summary>
    [HttpPost("api/desk/requests/{id:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ApproveServiceRequest(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new ApproveServiceRequestCommand(id);
            await _sender.Send(command, cancellationToken);
            return Ok(new { id, approved = true });
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
    /// Reject a service request that is pending approval.
    /// </summary>
    [HttpPost("api/desk/requests/{id:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RejectServiceRequest(
        Guid id,
        [FromBody] RejectServiceRequestApiRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new RejectServiceRequestCommand(id, request.Reason);
            await _sender.Send(command, cancellationToken);
            return Ok(new { id, rejected = true });
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
    /// Mark a service request as fulfilled.
    /// </summary>
    [HttpPost("api/desk/requests/{id:guid}/fulfill")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> FulfillServiceRequest(
        Guid id,
        [FromBody] FulfillServiceRequestApiRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new FulfillServiceRequestCommand(id, request.FulfillmentNotes);
            await _sender.Send(command, cancellationToken);
            return Ok(new { id, fulfilled = true });
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

    // ────────────────────────────── Request DTOs ──────────────────────────────

    /// <summary>Request body for creating a service request.</summary>
    public sealed record CreateServiceRequestApiRequest(
        string Title,
        string? Description,
        Guid CatalogItemId);

    /// <summary>Request body for rejecting a service request.</summary>
    public sealed record RejectServiceRequestApiRequest(string Reason);

    /// <summary>Request body for fulfilling a service request.</summary>
    public sealed record FulfillServiceRequestApiRequest(string? FulfillmentNotes);

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
