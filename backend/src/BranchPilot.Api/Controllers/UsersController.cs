using BranchPilot.Application.Common;
using BranchPilot.Application.Security;
using BranchPilot.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BranchPilot.Api.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private readonly UserManagementService _userManagementService;

    public UsersController(UserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    [Authorize(Policy = PermissionCodes.UsersView)]
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<TeamMemberResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<TeamMemberResponse>>> GetUsersAsync(
        [FromQuery] GetUsersRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _userManagementService.GetUsersAsync(request, cancellationToken));
    }

    [Authorize(Policy = PermissionCodes.UsersView)]
    [HttpGet("roles")]
    [ProducesResponseType(typeof(IReadOnlyCollection<RoleOptionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<RoleOptionResponse>>> GetRoleOptionsAsync(
        CancellationToken cancellationToken)
    {
        return Ok(await _userManagementService.GetRoleOptionsAsync(cancellationToken));
    }

    [Authorize(Policy = PermissionCodes.UsersManage)]
    [HttpPost]
    [ProducesResponseType(typeof(TeamMemberResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<TeamMemberResponse>> CreateUserAsync(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _userManagementService.CreateUserAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, user);
    }

    [Authorize(Policy = PermissionCodes.UsersManage)]
    [HttpPut("{userId:guid}/membership")]
    [ProducesResponseType(typeof(TeamMemberResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TeamMemberResponse>> UpdateMembershipAsync(
        Guid userId,
        [FromBody] UpdateUserMembershipRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _userManagementService.UpdateUserMembershipAsync(userId, request, cancellationToken));
    }
}
