using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WestCoastFitness.Application.Membership;

namespace WestCoastFitness.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/members")]
public sealed class MembersController(MembershipService membership) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<MemberProfile>> List(CancellationToken cancellationToken) =>
        await membership.ListMembersAsync(User.ToCaller(), cancellationToken);

    [HttpGet("{memberId:guid}")]
    public async Task<MemberProfile> Get(Guid memberId, CancellationToken cancellationToken) =>
        await membership.GetMemberAsync(User.ToCaller(), memberId, cancellationToken);

    [HttpGet("{memberId:guid}/summary")]
    public async Task<MembershipSummary> Summary(Guid memberId, CancellationToken cancellationToken) =>
        await membership.GetSummaryAsync(User.ToCaller(), memberId, cancellationToken);

    [HttpPut("{memberId:guid}/plan")]
    public async Task<PlanChangeResult> ChangePlan(Guid memberId, ChangePlanRequest request, CancellationToken cancellationToken) =>
        await membership.ChangePlanAsync(User.ToCaller(), memberId, request, cancellationToken);
}
