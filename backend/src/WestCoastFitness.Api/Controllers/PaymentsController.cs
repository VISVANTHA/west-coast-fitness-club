using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WestCoastFitness.Application.Payments;
using WestCoastFitness.Domain;

namespace WestCoastFitness.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/members/{memberId:guid}/payments")]
public sealed class PaymentsController(PaymentService payments) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<PaymentView>> List(Guid memberId, CancellationToken cancellationToken) =>
        await payments.ListForMemberAsync(User.ToCaller(), memberId, cancellationToken);

    [HttpPost]
    public async Task<ActionResult<PaymentView>> Charge(Guid memberId, ChargeRequest request, CancellationToken cancellationToken)
    {
        var receipt = await payments.ChargeAsync(User.ToCaller(), memberId, request, cancellationToken);
        var status = receipt.Status == PaymentStatus.Approved ? StatusCodes.Status201Created : StatusCodes.Status402PaymentRequired;
        return StatusCode(status, receipt);
    }
}
