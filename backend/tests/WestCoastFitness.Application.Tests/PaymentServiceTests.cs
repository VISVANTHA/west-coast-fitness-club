using WestCoastFitness.Application.Payments;
using WestCoastFitness.Domain;

namespace WestCoastFitness.Application.Tests;

public class PaymentServiceTests
{
    private static PaymentService Create(TestClub club, FakeGateway gateway) =>
        new(club.MemberRepo, club.PaymentRepo, gateway, club.UnitOfWork, club.Clock);

    [Fact]
    public async Task Charge_RecordsAnApprovedPayment()
    {
        var club = new TestClub();
        var member = club.AddMember();
        var gateway = new FakeGateway();

        var view = await Create(club, gateway).ChargeAsync(TestClub.MemberCaller(member), member.Id, new ChargeRequest(25m, "tok_visa", "Towel rental"), CancellationToken.None);

        Assert.Equal(PaymentStatus.Approved, view.Status);
        Assert.Equal("ref-ok", view.Reference);
        Assert.Equal("USD", Assert.Single(gateway.Requests).Currency);
        Assert.Single(club.Payments);
    }

    [Fact]
    public async Task Charge_RecordsADeclinedPayment()
    {
        var club = new TestClub();
        var member = club.AddMember();

        var view = await Create(club, new FakeGateway { Approve = false })
            .ChargeAsync(TestClub.MemberCaller(member), member.Id, new ChargeRequest(0m, "tok_visa", "Zero"), CancellationToken.None);

        Assert.Equal(PaymentStatus.Declined, view.Status);
        Assert.Equal(PaymentStatus.Declined, Assert.Single(club.Payments).Status);
    }

    [Fact]
    public async Task Charge_ForAnotherMember_IsForbiddenAndNeverReachesTheGateway()
    {
        var club = new TestClub();
        var owner = club.AddMember("Ana");
        var intruder = club.AddMember("Ben");
        var gateway = new FakeGateway();

        var ex = await Assert.ThrowsAsync<ClubException>(() =>
            Create(club, gateway).ChargeAsync(TestClub.MemberCaller(intruder), owner.Id, new ChargeRequest(10m, "tok", "x"), CancellationToken.None));

        Assert.Equal(ClubErrorKind.Forbidden, ex.Kind);
        Assert.Empty(gateway.Requests);
    }

    [Fact]
    public async Task Charge_UnknownMember_IsNotFound()
    {
        var club = new TestClub();

        var ex = await Assert.ThrowsAsync<ClubException>(() =>
            Create(club, new FakeGateway()).ChargeAsync(TestClub.AdminCaller(), Guid.NewGuid(), new ChargeRequest(10m, "tok", "x"), CancellationToken.None));

        Assert.Equal(ClubErrorKind.NotFound, ex.Kind);
    }

    [Fact]
    public async Task List_IsLimitedToTheOwner()
    {
        var club = new TestClub();
        var owner = club.AddMember("Ana");
        var other = club.AddMember("Ben");
        var service = Create(club, new FakeGateway());
        await service.ChargeAsync(TestClub.MemberCaller(owner), owner.Id, new ChargeRequest(10m, "tok", "x"), CancellationToken.None);

        var history = await service.ListForMemberAsync(TestClub.MemberCaller(owner), owner.Id, CancellationToken.None);
        var blocked = await Assert.ThrowsAsync<ClubException>(() => service.ListForMemberAsync(TestClub.MemberCaller(other), owner.Id, CancellationToken.None));

        Assert.Equal("x", Assert.Single(history).Description);
        Assert.Equal(ClubErrorKind.Forbidden, blocked.Kind);
    }
}
