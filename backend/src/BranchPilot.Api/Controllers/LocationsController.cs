using BranchPilot.Application.Locations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BranchPilot.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/locations")]
public sealed class LocationsController : ControllerBase
{
    private readonly LocationService _locationService;

    public LocationsController(LocationService locationService)
    {
        _locationService = locationService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<LocationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<LocationResponse>>> GetLocationsAsync(
        CancellationToken cancellationToken)
    {
        return Ok(await _locationService.GetLocationsAsync(cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType(typeof(LocationResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<LocationResponse>> CreateLocationAsync(
        [FromBody] CreateLocationRequest request,
        CancellationToken cancellationToken)
    {
        var location = await _locationService.CreateLocationAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, location);
    }
}
