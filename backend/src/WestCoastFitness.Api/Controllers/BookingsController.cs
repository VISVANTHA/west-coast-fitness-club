using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WestCoastFitness.Application.Scheduling;

namespace WestCoastFitness.Api.Controllers;

public sealed record BookClassRequest(Guid ClassId);

[ApiController]
[Authorize]
[Route("api")]
public sealed class BookingsController(BookingService bookings) : ControllerBase
{
    [HttpGet("members/{memberId:guid}/bookings")]
    public async Task<IReadOnlyList<BookingView>> List(Guid memberId, CancellationToken cancellationToken) =>
        await bookings.ListForMemberAsync(User.ToCaller(), memberId, cancellationToken);

    [HttpPost("members/{memberId:guid}/bookings")]
    public async Task<ActionResult<BookingView>> Book(Guid memberId, BookClassRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await bookings.BookAsync(User.ToCaller(), memberId, request.ClassId, cancellationToken));

    [HttpDelete("bookings/{bookingId:guid}")]
    public async Task<IActionResult> Cancel(Guid bookingId, CancellationToken cancellationToken)
    {
        await bookings.CancelAsync(User.ToCaller(), bookingId, cancellationToken);
        return NoContent();
    }
}
