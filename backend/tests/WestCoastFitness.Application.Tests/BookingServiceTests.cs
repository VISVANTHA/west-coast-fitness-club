using WestCoastFitness.Application.Scheduling;
using WestCoastFitness.Application.Security;
using WestCoastFitness.Domain;

namespace WestCoastFitness.Application.Tests;

public class BookingServiceTests
{
    private static BookingService Create(TestClub club) =>
        new(club.MemberRepo, club.PlanRepo, club.ClassRepo, club.BookingRepo, club.UnitOfWork, club.Clock);

    [Fact]
    public async Task Book_ConfirmsAndReturnsTheBooking()
    {
        var club = new TestClub();
        var member = club.AddMember();
        var fitnessClass = club.AddClass(TestClub.Now.AddDays(1));

        var view = await Create(club).BookAsync(TestClub.MemberCaller(member), member.Id, fitnessClass.Id, CancellationToken.None);

        Assert.Equal(BookingStatus.Confirmed, view.Status);
        Assert.Equal(fitnessClass.Title, view.Title);
        Assert.Single(club.Bookings);
        Assert.Equal(1, club.SaveCount);
    }

    [Fact]
    public async Task Book_ByAnotherMember_IsForbidden()
    {
        var club = new TestClub();
        var owner = club.AddMember("Ana");
        var intruder = club.AddMember("Ben");
        var fitnessClass = club.AddClass(TestClub.Now.AddDays(1));

        var ex = await Assert.ThrowsAsync<ClubException>(() =>
            Create(club).BookAsync(TestClub.MemberCaller(intruder), owner.Id, fitnessClass.Id, CancellationToken.None));

        Assert.Equal(ClubErrorKind.Forbidden, ex.Kind);
        Assert.Empty(club.Bookings);
    }

    [Fact]
    public async Task Book_ByAnAdministrator_IsAllowed()
    {
        var club = new TestClub();
        var guest = club.AddMember("Gia");
        var evening = club.AddClass(TestClub.Now.AddDays(2).AddHours(6));

        var booked = await Create(club).BookAsync(TestClub.AdminCaller(), guest.Id, evening.Id, CancellationToken.None);

        Assert.Equal(evening.Id, booked.FitnessClassId);
    }

    [Fact]
    public async Task Book_EnforcesTheBookingRules()
    {
        var club = new TestClub();
        var member = club.AddMember();
        var fullClass = club.AddClass(TestClub.Now.AddDays(1), capacity: 1);
        club.AddBooking(club.AddMember("Cy"), fullClass);

        var ex = await Assert.ThrowsAsync<ClubException>(() =>
            Create(club).BookAsync(TestClub.MemberCaller(member), member.Id, fullClass.Id, CancellationToken.None));

        Assert.Equal(ClubErrorKind.Conflict, ex.Kind);
        Assert.Contains("full", ex.Message);
    }

    [Fact]
    public async Task Book_UnknownMemberOrClass_IsNotFound()
    {
        var club = new TestClub();
        var member = club.AddMember();
        var service = Create(club);

        var missingMember = Guid.NewGuid();
        var noMember = await Assert.ThrowsAsync<ClubException>(() =>
            service.BookAsync(TestClub.AdminCaller(), missingMember, Guid.NewGuid(), CancellationToken.None));
        var noClass = await Assert.ThrowsAsync<ClubException>(() =>
            service.BookAsync(TestClub.MemberCaller(member), member.Id, Guid.NewGuid(), CancellationToken.None));

        Assert.Equal(ClubErrorKind.NotFound, noMember.Kind);
        Assert.Equal(ClubErrorKind.NotFound, noClass.Kind);
    }

    [Fact]
    public async Task Book_WithAMissingPlan_IsAConflict()
    {
        var club = new TestClub();
        var member = club.AddMember();
        var fitnessClass = club.AddClass(TestClub.Now.AddDays(1));
        club.Plans.Clear();

        var ex = await Assert.ThrowsAsync<ClubException>(() =>
            Create(club).BookAsync(TestClub.MemberCaller(member), member.Id, fitnessClass.Id, CancellationToken.None));

        Assert.Equal(ClubErrorKind.Conflict, ex.Kind);
    }

    [Fact]
    public async Task Cancel_MarksTheBookingCancelled()
    {
        var club = new TestClub();
        var member = club.AddMember();
        var booking = club.AddBooking(member, club.AddClass(TestClub.Now.AddDays(1)));

        await Create(club).CancelAsync(TestClub.MemberCaller(member), booking.Id, CancellationToken.None);

        Assert.Equal(BookingStatus.Cancelled, booking.Status);
    }

    [Fact]
    public async Task Cancel_AnotherMembersBooking_IsForbidden()
    {
        var club = new TestClub();
        var owner = club.AddMember("Ana");
        var intruder = club.AddMember("Ben");
        var booking = club.AddBooking(owner, club.AddClass(TestClub.Now.AddDays(1)));

        var ex = await Assert.ThrowsAsync<ClubException>(() =>
            Create(club).CancelAsync(TestClub.MemberCaller(intruder), booking.Id, CancellationToken.None));

        Assert.Equal(ClubErrorKind.Forbidden, ex.Kind);
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
    }

    [Fact]
    public async Task Cancel_TwiceOrUnknown_Fails()
    {
        var club = new TestClub();
        var member = club.AddMember();
        var booking = club.AddBooking(member, club.AddClass(TestClub.Now.AddDays(1)), BookingStatus.Cancelled);
        var service = Create(club);

        var again = await Assert.ThrowsAsync<ClubException>(() => service.CancelAsync(TestClub.MemberCaller(member), booking.Id, CancellationToken.None));
        var unknown = await Assert.ThrowsAsync<ClubException>(() => service.CancelAsync(TestClub.MemberCaller(member), Guid.NewGuid(), CancellationToken.None));

        Assert.Equal(ClubErrorKind.Conflict, again.Kind);
        Assert.Equal(ClubErrorKind.NotFound, unknown.Kind);
    }

    [Fact]
    public async Task List_ReturnsOnlyTheMembersBookings_AndBlocksOthers()
    {
        var club = new TestClub();
        var owner = club.AddMember("Ana");
        var other = club.AddMember("Ben");
        club.AddBooking(owner, club.AddClass(TestClub.Now.AddDays(1)));
        club.AddBooking(other, club.AddClass(TestClub.Now.AddDays(2)));
        var service = Create(club);

        var mine = await service.ListForMemberAsync(TestClub.MemberCaller(owner), owner.Id, CancellationToken.None);
        var denied = await Assert.ThrowsAsync<ClubException>(() => service.ListForMemberAsync(TestClub.MemberCaller(other), owner.Id, CancellationToken.None));

        Assert.Single(mine);
        Assert.Equal(ClubErrorKind.Forbidden, denied.Kind);
    }

    [Fact]
    public void AccessPolicy_RequiresAnAdministratorForStaffActions()
    {
        var member = new TestClub().AddMember();

        AccessPolicy.EnsureAdministrator(TestClub.AdminCaller());
        var ex = Assert.Throws<ClubException>(() => AccessPolicy.EnsureAdministrator(TestClub.MemberCaller(member)));

        Assert.Equal(ClubErrorKind.Forbidden, ex.Kind);
        Assert.Throws<ArgumentNullException>(() => AccessPolicy.EnsureAdministrator(null!));
        Assert.Throws<ArgumentNullException>(() => AccessPolicy.EnsureCanAccessMember(null!, Guid.NewGuid()));
    }
}
