using WestCoastFitness.Application.Payments;
using WestCoastFitness.Application.Security;
using WestCoastFitness.Domain;

namespace WestCoastFitness.Application.Membership;

public sealed record MemberProfile(Guid Id, string FullName, string Email, string Phone, Guid MembershipPlanId, DateTimeOffset JoinedOn);

public sealed record MembershipSummary(Guid MemberId, string FullName, string PlanName, decimal MonthlyPrice, DateTimeOffset JoinedOn);

public sealed record ChangePlanRequest(Guid NewPlanId, string PaymentToken);

public sealed record PlanChangeResult(Guid MembershipPlanId, decimal AmountCharged);

public sealed class MembershipService(
    IMemberRepository members,
    IMembershipPlanRepository plans,
    PaymentService paymentService,
    IUnitOfWork unitOfWork,
    TimeProvider clock)
{
    public async Task<IReadOnlyList<MemberProfile>> ListMembersAsync(CallerContext caller, CancellationToken cancellationToken)
    {
        AccessPolicy.EnsureAdministrator(caller);
        return (await members.ListAsync(cancellationToken)).Select(ToProfile).ToList();
    }

    public async Task<MemberProfile> GetMemberAsync(CallerContext caller, Guid memberId, CancellationToken cancellationToken)
    {
        AccessPolicy.EnsureCanAccessMember(caller, memberId);
        var member = await members.FindAsync(memberId, cancellationToken)
            ?? throw ClubException.NotFound("Member not found.");
        return ToProfile(member);
    }

    public async Task<MembershipSummary> GetSummaryAsync(CallerContext caller, Guid memberId, CancellationToken cancellationToken)
    {
        AccessPolicy.EnsureCanAccessMember(caller, memberId);
        var member = await members.FindAsync(memberId, cancellationToken)
            ?? throw ClubException.NotFound("Member not found.");
        var plan = await plans.FindAsync(member.MembershipPlanId, cancellationToken)
            ?? throw ClubException.Conflict("The member's plan no longer exists.");
        return new MembershipSummary(member.Id, member.FullName, plan.Name, plan.MonthlyPrice, member.JoinedOn);
    }

    public async Task<PlanChangeResult> ChangePlanAsync(CallerContext caller, Guid memberId, ChangePlanRequest request, CancellationToken cancellationToken)
    {
        AccessPolicy.EnsureCanAccessMember(caller, memberId);
        ArgumentNullException.ThrowIfNull(request);

        var member = await members.FindAsync(memberId, cancellationToken)
            ?? throw ClubException.NotFound("Member not found.");
        var current = await plans.FindAsync(member.MembershipPlanId, cancellationToken)
            ?? throw ClubException.Conflict("The member's plan no longer exists.");
        var target = await plans.FindAsync(request.NewPlanId, cancellationToken)
            ?? throw ClubException.NotFound("Plan not found.");

        if (target.Id == current.Id)
        {
            throw ClubException.Conflict("The member is already on this plan.");
        }

        var amount = ProrationCalculator.Calculate(current.MonthlyPrice, target.MonthlyPrice, clock.GetUtcNow());
        if (amount > 0m)
        {
            var payment = await paymentService.ProcessAsync(
                memberId,
                new ChargeRequest(amount, request.PaymentToken, $"Upgrade to {target.Name}"),
                cancellationToken);
            if (payment.Status != PaymentStatus.Approved)
            {
                throw ClubException.PaymentDeclined("The payment for the plan change was declined.");
            }
        }

        member.MembershipPlanId = target.Id;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new PlanChangeResult(target.Id, amount);
    }

    private static MemberProfile ToProfile(Member member) =>
        new(member.Id, member.FullName, member.Email, member.Phone, member.MembershipPlanId, member.JoinedOn);
}
