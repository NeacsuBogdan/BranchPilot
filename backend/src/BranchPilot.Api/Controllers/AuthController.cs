using BranchPilot.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BranchPilot.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("register-organization")]
    [ProducesResponseType(typeof(AuthenticatedSessionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthenticatedSessionResponse>> RegisterOrganizationAsync(
        [FromBody] RegisterOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _authService.RegisterOrganizationAsync(request, cancellationToken));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthenticatedSessionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthenticatedSessionResponse>> LoginAsync(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _authService.LoginAsync(request, cancellationToken));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthenticatedSessionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthenticatedSessionResponse>> RefreshAsync(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _authService.RefreshAsync(request, cancellationToken));
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> LogoutAsync(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        await _authService.LogoutAsync(request, cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(CurrentSessionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CurrentSessionResponse>> GetCurrentSessionAsync(CancellationToken cancellationToken)
    {
        return Ok(await _authService.GetCurrentSessionAsync(cancellationToken));
    }
}
