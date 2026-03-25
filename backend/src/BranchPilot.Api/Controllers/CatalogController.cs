using BranchPilot.Application.Catalog;
using BranchPilot.Application.Common;
using BranchPilot.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BranchPilot.Api.Controllers;

[ApiController]
[Route("api/catalog")]
public sealed class CatalogController : ControllerBase
{
    private readonly CatalogService _catalogService;

    public CatalogController(CatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    [Authorize(Policy = PermissionCodes.CatalogView)]
    [HttpGet("options")]
    [ProducesResponseType(typeof(CatalogOptionsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CatalogOptionsResponse>> GetOptionsAsync(CancellationToken cancellationToken)
    {
        return Ok(await _catalogService.GetCatalogOptionsAsync(cancellationToken));
    }

    [Authorize(Policy = PermissionCodes.CatalogView)]
    [HttpGet("categories")]
    [ProducesResponseType(typeof(PagedResponse<CategoryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<CategoryResponse>>> GetCategoriesAsync(
        [FromQuery] GetCatalogReferencesRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _catalogService.GetCategoriesAsync(request, cancellationToken));
    }

    [Authorize(Policy = PermissionCodes.CatalogManage)]
    [HttpPost("categories")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<CategoryResponse>> CreateCategoryAsync(
        [FromBody] CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        return StatusCode(StatusCodes.Status201Created, await _catalogService.CreateCategoryAsync(request, cancellationToken));
    }

    [Authorize(Policy = PermissionCodes.CatalogView)]
    [HttpGet("tax-profiles")]
    [ProducesResponseType(typeof(PagedResponse<TaxProfileResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<TaxProfileResponse>>> GetTaxProfilesAsync(
        [FromQuery] GetCatalogReferencesRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _catalogService.GetTaxProfilesAsync(request, cancellationToken));
    }

    [Authorize(Policy = PermissionCodes.CatalogManage)]
    [HttpPost("tax-profiles")]
    [ProducesResponseType(typeof(TaxProfileResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<TaxProfileResponse>> CreateTaxProfileAsync(
        [FromBody] CreateTaxProfileRequest request,
        CancellationToken cancellationToken)
    {
        return StatusCode(StatusCodes.Status201Created, await _catalogService.CreateTaxProfileAsync(request, cancellationToken));
    }

    [Authorize(Policy = PermissionCodes.CatalogView)]
    [HttpGet("items")]
    [ProducesResponseType(typeof(CatalogItemPageResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CatalogItemPageResponse>> GetCatalogItemsAsync(
        [FromQuery] GetCatalogItemsRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _catalogService.GetCatalogItemsAsync(request, cancellationToken));
    }

    [Authorize(Policy = PermissionCodes.CatalogView)]
    [HttpGet("items/{itemId:guid}")]
    [ProducesResponseType(typeof(CatalogItemDetailsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CatalogItemDetailsResponse>> GetCatalogItemAsync(
        Guid itemId,
        CancellationToken cancellationToken)
    {
        return Ok(await _catalogService.GetCatalogItemAsync(itemId, cancellationToken));
    }

    [Authorize(Policy = PermissionCodes.CatalogManage)]
    [HttpPost("items")]
    [ProducesResponseType(typeof(CatalogItemDetailsResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<CatalogItemDetailsResponse>> CreateCatalogItemAsync(
        [FromBody] UpsertCatalogItemRequest request,
        CancellationToken cancellationToken)
    {
        return StatusCode(StatusCodes.Status201Created, await _catalogService.CreateCatalogItemAsync(request, cancellationToken));
    }

    [Authorize(Policy = PermissionCodes.CatalogManage)]
    [HttpPut("items/{itemId:guid}")]
    [ProducesResponseType(typeof(CatalogItemDetailsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CatalogItemDetailsResponse>> UpdateCatalogItemAsync(
        Guid itemId,
        [FromBody] UpsertCatalogItemRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _catalogService.UpdateCatalogItemAsync(itemId, request, cancellationToken));
    }
}
