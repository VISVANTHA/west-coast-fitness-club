using WestCoastFitness.Application.Scheduling;
using WestCoastFitness.Domain;

namespace WestCoastFitness.Application.Tests;

public class BookingRulesTests
{
    private static BookingDecision Evaluate(TestClub club, FitnessClass target, MembershipPlan plan, int booked = 0, params ClassBooking[] active) =>
        BookingRules.Evaluate(new BookingContext(target, active, booked, plan, TestClub.Now));

    [Fact]
    public void Allows_AnOrdinaryBooking()
    {
        var club = new TestClub();
        var target = club.AddClass(TestClub.Now.AddDays(1));

        var decision = Evaluate(club, target, club.Basic);

        Assert.True(decision.Allowed);
        Assert.Null(decision.Reason);
    }

    [Fact]
    public void Denies_AClassThatAlreadyStarted()
    {
        var club = new TestClub();
        var target = club.AddClass(TestClub.Now.AddHours(-1));

        var decision = Evaluate(club, target, club.Basic);

        Assert.False(decision.Allowed);
        Assert.Contains("already started", decision.Reason);
    }

    [Fact]
    public void Denies_AFullClass()
    {
        var club = new TestClub();
        var target = club.AddClass(TestClub.Now.AddDays(1), capacity: 2);

        var decision = Evaluate(club, target, club.Basic, booked: 2);

        Assert.False(decision.Allowed);
        Assert.Contains("full", decision.Reason);
    }

    [Fact]
    public void Denies_ADuplicateBooking()
    {
        var club = new TestClub();
        var member = club.AddMember();
        var target = club.AddClass(TestClub.Now.AddDays(1));
        var existing = club.AddBooking(member, target);

        var decision = Evaluate(club, target, club.Basic, 1, existing);

        Assert.False(decision.Allowed);
        Assert.Contains("already booked", decision.Reason);
    }

    [Fact]
    public void Denies_AnOverlappingClass()
    {
        var club = new TestClub();
        var member = club.AddMember();
        var first = club.AddClass(TestClub.Now.AddDays(1));
        var overlapping = club.AddClass(TestClub.Now.AddDays(1).AddMinutes(30));
        var existing = club.AddBooking(member, first);

        var decision = Evaluate(club, overlapping, club.Premium, 0, existing);

        Assert.False(decision.Allowed);
        Assert.Contains(first.Title, decision.Reason);
    }

    [Fact]
    public void Allows_BackToBackClasses()
    {
        var club = new TestClub();
        var member = club.AddMember();
        var first = club.AddClass(TestClub.Now.AddDays(1));
        var next = club.AddClass(first.EndsAt);
        var existing = club.AddBooking(member, first);

        Assert.True(Evaluate(club, next, club.Premium, 0, existing).Allowed);
    }

    [Fact]
    public void Denies_WhenTheWeeklyLimitIsReached()
    {
        var club = new TestClub();
        var member = club.AddMember();
        var tuesday = club.AddClass(TestClub.Now.AddDays(1));
        var wednesday = club.AddClass(TestClub.Now.AddDays(2));
        var thursday = club.AddClass(TestClub.Now.AddDays(3));
        var active = new[] { club.AddBooking(member, tuesday), club.AddBooking(member, wednesday) };

        var decision = Evaluate(club, thursday, club.Basic, 0, active);

        Assert.False(decision.Allowed);
        Assert.Contains("weekly", decision.Reason);
    }

    [Fact]
    public void WeeklyLimit_IgnoresOtherWeeks()
    {
        var club = new TestClub();
        var member = club.AddMember();
        var nextWeek = club.AddClass(TestClub.Now.AddDays(8));
        var nextWeekToo = club.AddClass(TestClub.Now.AddDays(9));
        var thisWeek = club.AddClass(TestClub.Now.AddDays(2));
        var active = new[] { club.AddBooking(member, nextWeek), club.AddBooking(member, nextWeekToo) };

        Assert.True(Evaluate(club, thisWeek, club.Basic, 0, active).Allowed);
    }

    [Fact]
    public void UnlimitedPlan_IsNeverCapped()
    {
        var club = new TestClub();
        var member = club.AddMember(plan: club.Premium);
        var active = Enumerable.Range(1, 6)
            .Select(d => club.AddBooking(member, club.AddClass(TestClub.Now.AddDays(d).AddHours(d))))
            .ToArray();
        var target = club.AddClass(TestClub.Now.AddDays(6).AddHours(10));

        Assert.True(Evaluate(club, target, club.Premium, 0, active).Allowed);
    }

    [Theory]
    [InlineData(2030, 1, 7, 2030, 1, 7)]
    [InlineData(2030, 1, 9, 2030, 1, 7)]
    [InlineData(2030, 1, 13, 2030, 1, 7)]
    [InlineData(2030, 1, 14, 2030, 1, 14)]
    public void WeekStart_IsTheMondayOfThatWeek(int year, int month, int day, int expectedYear, int expectedMonth, int expectedDay)
    {
        var start = BookingRules.WeekStart(new DateTimeOffset(year, month, day, 15, 30, 0, TimeSpan.Zero));

        Assert.Equal(new DateTimeOffset(expectedYear, expectedMonth, expectedDay, 0, 0, 0, TimeSpan.Zero), start);
    }

    [Fact]
    public void Evaluate_NullContextThrows()
    {
        Assert.Throws<ArgumentNullException>(() => BookingRules.Evaluate(null!));
    }
}
