using WestCoastFitness.Application.Membership;
using WestCoastFitness.Application.Payments;
using WestCoastFitness.Domain;

namespace WestCoastFitness.Application.Tests;

public class MembershipServiceTests
{
    private static (MembershipService Service, FakeGateway Gateway) Create(TestClub club)
    {
        var gateway = new FakeGateway();
        var payments = new PaymentService(club.MemberRepo, club.PaymentRepo, gateway, club.UnitOfWork, club.Clock);
        return (new MembershipService(club.MemberRepo, club.PlanRepo, payments, club.UnitOfWork, club.Clock), gateway);
    }

    [Fact]
    public async Task List_IsAdministratorOnly()
    {
        var club = new TestClub();
        var member = club.AddMember();
        var (service, _) = Create(club);

        Assert.Single(await service.ListMembersAsync(TestClub.AdminCaller(), CancellationToken.None));
        var ex = await Assert.ThrowsAsync<ClubException>(() => service.ListMembersAsync(TestClub.MemberCaller(member), CancellationToken.None));

        Assert.Equal(ClubErrorKind.Forbidden, ex.Kind);
    }

    [Fact]
    public async Task Get_ReturnsTheOwnProfile_AndBlocksOthers()
    {
        var club = new TestClub();
        var owner = club.AddMember("Ana");
        var other = club.AddMember("Ben");
        var (service, _) = Create(club);

        var profile = await service.GetMemberAsync(TestClub.MemberCaller(owner), owner.Id, CancellationToken.None);
        var denied = await Assert.ThrowsAsync<ClubException>(() => service.GetMemberAsync(TestClub.MemberCaller(other), owner.Id, CancellationToken.None));

        Assert.Equal("Ana Reyes", profile.FullName);
        Assert.Equal(ClubErrorKind.Forbidden, denied.Kind);
    }

    [Fact]
    public async Task Get_UnknownMember_IsNotFound()
    {
        var (service, _) = Create(new TestClub());

        var ex = await Assert.ThrowsAsync<ClubException>(() => service.GetMemberAsync(TestClub.AdminCaller(), Guid.NewGuid(), CancellationToken.None));

        Assert.Equal(ClubErrorKind.NotFound, ex.Kind);
    }

    [Fact]
    public async Task Summary_ReturnsThePlan_AndHasNoObjectLevelCheck_KnownGap()
    {
        var club = new TestClub();
        var owner = club.AddMember("Ana");
        var other = club.AddMember("Ben");
        var (service, _) = Create(club);

        var summary = await service.GetSummaryAsync(TestClub.MemberCaller(owner), owner.Id, CancellationToken.None);
        // Seeded BOLA gap (see docs/clarifications.md): another member can read this summary.
        var leaked = await service.GetSummaryAsync(TestClub.MemberCaller(other), owner.Id, CancellationToken.None);

        Assert.Equal("Basic", summary.PlanName);
        Assert.Equal(30m, summary.MonthlyPrice);
        Assert.Equal("Ana Reyes", leaked.FullName);
    }

    [Fact]
    public async Task Summary_WithAMissingPlanOrMember_Fails()
    {
        var club = new TestClub();
        var member = club.AddMember();
        var (service, _) = Create(club);
        var missing = await Assert.ThrowsAsync<ClubException>(() => service.GetSummaryAsync(TestClub.AdminCaller(), Guid.NewGuid(), CancellationToken.None));
        club.Plans.Clear();

        var noPlan = await Assert.ThrowsAsync<ClubException>(() => service.GetSummaryAsync(TestClub.AdminCaller(), member.Id, CancellationToken.None));

        Assert.Equal(ClubErrorKind.NotFound, missing.Kind);
        Assert.Equal(ClubErrorKind.Conflict, noPlan.Kind);
    }

    [Fact]
    public async Task ChangePlan_Upgrade_ChargesTheProratedDifference()
    {
        var club = new TestClub();
        var member = club.AddMember();
        var (service, gateway) = Create(club);

        var result = await service.ChangePlanAsync(TestClub.MemberCaller(member), member.Id, new ChangePlanRequest(club.Premium.Id, "tok_visa"), CancellationToken.None);

        // 2030-01-07: 25 of 31 days remain, so 30.00 * 25 / 31 = 24.19.
        Assert.Equal(24.19m, result.AmountCharged);
        Assert.Equal(club.Premium.Id, member.MembershipPlanId);
        Assert.Equal(24.19m, Assert.Single(gateway.Requests).Amount);
        Assert.Single(club.Payments);
    }

    [Fact]
    public async Task ChangePlan_Downgrade_IsFreeAndSkipsTheGateway()
    {
        var club = new TestClub();
        var member = club.AddMember(plan: club.Premium);
        var (service, gateway) = Create(club);

        var result = await service.ChangePlanAsync(TestClub.MemberCaller(member), member.Id, new ChangePlanRequest(club.Basic.Id, "tok_visa"), CancellationToken.None);

        Assert.Equal(0m, result.AmountCharged);
        Assert.Empty(gateway.Requests);
        Assert.Equal(club.Basic.Id, member.MembershipPlanId);
    }

    [Fact]
    public async Task ChangePlan_DeclinedPayment_LeavesThePlanUnchanged()
    {
        var club = new TestClub();
        var member = club.AddMember();
        var (service, gateway) = Create(club);
        gateway.Approve = false;

        var ex = await Assert.ThrowsAsync<ClubException>(() =>
            service.ChangePlanAsync(TestClub.MemberCaller(member), member.Id, new ChangePlanRequest(club.Premium.Id, "tok_decline"), CancellationToken.None));

        Assert.Equal(ClubErrorKind.PaymentDeclined, ex.Kind);
        Assert.Equal(club.Basic.Id, member.MembershipPlanId);
        Assert.Equal(PaymentStatus.Declined, Assert.Single(club.Payments).Status);
    }

    [Fact]
    public async Task ChangePlan_RejectsSamePlanUnknownPlanAndOtherMembers()
    {
        var club = new TestClub();
        var owner = club.AddMember("Ana");
        var other = club.AddMember("Ben");
        var (service, _) = Create(club);

        var same = await Assert.ThrowsAsync<ClubException>(() =>
            service.ChangePlanAsync(TestClub.MemberCaller(owner), owner.Id, new ChangePlanRequest(club.Basic.Id, "t"), CancellationToken.None));
        var unknown = await Assert.ThrowsAsync<ClubException>(() =>
            service.ChangePlanAsync(TestClub.MemberCaller(owner), owner.Id, new ChangePlanRequest(Guid.NewGuid(), "t"), CancellationToken.None));
        var denied = await Assert.ThrowsAsync<ClubException>(() =>
            service.ChangePlanAsync(TestClub.MemberCaller(other), owner.Id, new ChangePlanRequest(club.Premium.Id, "t"), CancellationToken.None));
        var missing = await Assert.ThrowsAsync<ClubException>(() =>
            service.ChangePlanAsync(TestClub.AdminCaller(), Guid.NewGuid(), new ChangePlanRequest(club.Premium.Id, "t"), CancellationToken.None));

        Assert.Equal(ClubErrorKind.Conflict, same.Kind);
        Assert.Equal(ClubErrorKind.NotFound, unknown.Kind);
        Assert.Equal(ClubErrorKind.Forbidden, denied.Kind);
        Assert.Equal(ClubErrorKind.NotFound, missing.Kind);
    }
}
