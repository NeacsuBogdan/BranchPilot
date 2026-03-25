using BranchPilot.Application.Bookings;
using BranchPilot.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BranchPilot.Api.Controllers;

[ApiController]
[Route("api/bookings")]
public sealed class BookingsController : ControllerBase
{
    private readonly BookingService _bookingService;

    public BookingsController(BookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [Authorize(Policy = PermissionCodes.BookingsView)]
    [HttpGet("options")]
    [ProducesResponseType(typeof(BookingOptionsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BookingOptionsResponse>> GetBookingOptionsAsync(CancellationToken cancellationToken)
    {
        return Ok(await _bookingService.GetBookingOptionsAsync(cancellationToken));
    }

    [Authorize(Policy = PermissionCodes.BookingsView)]
    [HttpGet]
    [ProducesResponseType(typeof(BookingPageResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BookingPageResponse>> GetBookingsAsync(
        [FromQuery] GetBookingsRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _bookingService.GetBookingsAsync(request, cancellationToken));
    }

    [Authorize(Policy = PermissionCodes.BookingsView)]
    [HttpGet("{bookingId:guid}")]
    [ProducesResponseType(typeof(BookingDetailsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BookingDetailsResponse>> GetBookingAsync(
        Guid bookingId,
        CancellationToken cancellationToken)
    {
        return Ok(await _bookingService.GetBookingAsync(bookingId, cancellationToken));
    }

    [Authorize(Policy = PermissionCodes.BookingsManage)]
    [HttpPost]
    [ProducesResponseType(typeof(BookingDetailsResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<BookingDetailsResponse>> CreateBookingAsync(
        [FromBody] CreateBookingRequest request,
        CancellationToken cancellationToken)
    {
        return StatusCode(StatusCodes.Status201Created, await _bookingService.CreateBookingAsync(request, cancellationToken));
    }

    [Authorize(Policy = PermissionCodes.BookingsManage)]
    [HttpPost("{bookingId:guid}/confirm")]
    [ProducesResponseType(typeof(BookingDetailsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BookingDetailsResponse>> ConfirmBookingAsync(
        Guid bookingId,
        CancellationToken cancellationToken)
    {
        return Ok(await _bookingService.ConfirmBookingAsync(bookingId, cancellationToken));
    }

    [Authorize(Policy = PermissionCodes.BookingsManage)]
    [HttpPost("{bookingId:guid}/complete")]
    [ProducesResponseType(typeof(BookingDetailsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BookingDetailsResponse>> CompleteBookingAsync(
        Guid bookingId,
        CancellationToken cancellationToken)
    {
        return Ok(await _bookingService.CompleteBookingAsync(bookingId, cancellationToken));
    }

    [Authorize(Policy = PermissionCodes.BookingsManage)]
    [HttpPost("{bookingId:guid}/reschedule")]
    [ProducesResponseType(typeof(BookingDetailsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BookingDetailsResponse>> RescheduleBookingAsync(
        Guid bookingId,
        [FromBody] RescheduleBookingRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _bookingService.RescheduleBookingAsync(bookingId, request, cancellationToken));
    }

    [Authorize(Policy = PermissionCodes.BookingsManage)]
    [HttpPost("{bookingId:guid}/cancel")]
    [ProducesResponseType(typeof(BookingDetailsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BookingDetailsResponse>> CancelBookingAsync(
        Guid bookingId,
        [FromBody] CancelBookingRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _bookingService.CancelBookingAsync(bookingId, request, cancellationToken));
    }
}
