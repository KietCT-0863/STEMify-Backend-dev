using MediatR;
using Microsoft.AspNetCore.Mvc;
using Order.Application.Queries.Organizations.GetOrganizationById;
using Order.Application.Queries.Organizations.GetOrganizationList;
using Shared.Protos.Order;

namespace Order.API.Controllers;

[ApiController]
public class OrganizationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<OrganizationsController> _logger;

    public OrganizationsController(IMediator mediator, ILogger<OrganizationsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated list of organizations
    /// </summary>
    [HttpGet]
    [Route("api/organizations")]
    [Route("api/v1/organizations")]
    [ProducesResponseType(typeof(GrpcPagedOrganizationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GrpcPagedOrganizationResponse>> GetOrganizations(
        [FromQuery] string? search = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? orderBy = null,
        [FromQuery] bool isDescending = false,
        [FromQuery] int? organizationTypeId = null,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Domain.Enums.OrganizationStatus? statusEnum = null;
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<Domain.Enums.OrganizationStatus>(status, true, out var parsedStatus))
            {
                statusEnum = parsedStatus;
            }

            var query = new GetOrganizationListQuery
            {
                Search = search,
                PageNumber = pageNumber,
                PageSize = pageSize,
                OrderBy = orderBy,
                IsDescending = isDescending,
                OrganizationTypeId = organizationTypeId,
                Status = statusEnum
            };

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting organizations list");
            return StatusCode(500, new { error = "An error occurred while retrieving organizations" });
        }
    }

    /// <summary>
    /// Get organization by ID
    /// </summary>
    [HttpGet("{id}")]
    [Route("api/organizations/{id}")]
    [Route("api/v1/organizations/{id}")]
    [ProducesResponseType(typeof(GrpcOrganizationDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GrpcOrganizationDetail>> GetOrganizationById(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetOrganizationByIdQuery { Id = id };
            var result = await _mediator.Send(query, cancellationToken);
            
            if (result == null)
            {
                return NotFound(new { error = $"Organization with ID {id} not found" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting organization by ID {OrganizationId}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving the organization" });
        }
    }
}
