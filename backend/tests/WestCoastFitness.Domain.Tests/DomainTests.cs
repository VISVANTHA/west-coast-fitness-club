using WestCoastFitness.Domain;

namespace WestCoastFitness.Domain.Tests;

public class DomainTests
{
    private static readonly DateTimeOffset Start = new(2030, 1, 7, 17, 0, 0, TimeSpan.Zero);

    private static FitnessClass ClassAt(DateTimeOffset start, int minutes = 60) =>
        new() { Id = Guid.NewGuid(), Title = "Spin", StartsAt = start, DurationMinutes = minutes, Capacity = 10 };

    [Fact]
    public void EndsAt_AddsDuration()
    {
        Assert.Equal(Start.AddMinutes(45), ClassAt(Start, 45).EndsAt);
    }

    [Theory]
    [InlineData(30, true)]
    [InlineData(59, true)]
    [InlineData(60, false)]
    [InlineData(-30, true)]
    [InlineData(-60, false)]
    public void OverlapsWith_DetectsOverlapAndTouchingEdges(int offsetMinutes, bool expected)
    {
        var first = ClassAt(Start);
        var second = ClassAt(Start.AddMinutes(offsetMinutes));

        Assert.Equal(expected, first.OverlapsWith(second));
    }

    [Fact]
    public void OverlapsWith_NullThrows()
    {
        Assert.Throws<ArgumentNullException>(() => ClassAt(Start).OverlapsWith(null!));
    }

    [Fact]
    public void Booking_CancelMarksInactive()
    {
        var booking = new ClassBooking { Status = BookingStatus.Confirmed };
        Assert.True(booking.IsActive);

        booking.Cancel();

        Assert.False(booking.IsActive);
        Assert.Equal(BookingStatus.Cancelled, booking.Status);
    }

    [Fact]
    public void Member_FullNameJoinsNames()
    {
        var member = new Member { FirstName = "Ana", LastName = "Reyes" };
        Assert.Equal("Ana Reyes", member.FullName);
    }

    [Fact]
    public void Defaults_AreSafe()
    {
        Assert.Equal("USD", new Payment().Currency);
        Assert.Equal(UserRole.Member, new UserAccount().Role);
        Assert.Null(new MembershipPlan().MaxClassesPerWeek);
        Assert.Equal(string.Empty, new Trainer().Specialty);
    }
}
