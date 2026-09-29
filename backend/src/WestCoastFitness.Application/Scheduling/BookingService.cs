using WestCoastFitness.Application.Security;
using WestCoastFitness.Domain;

namespace WestCoastFitness.Application.Scheduling;

public sealed record BookingView(Guid Id, Guid FitnessClassId, string Title, string Location, DateTimeOffset StartsAt, BookingStatus Status);

public sealed class BookingService(
    IMemberRepository members,
    IMembershipPlanRepository plans,
    IFitnessClassRepository classes,
    IBookingRepository bookings,
    IUnitOfWork unitOfWork,
    TimeProvider clock)
{
    public async Task<BookingView> BookAsync(CallerContext caller, Guid memberId, Guid classId, CancellationToken cancellationToken)
    {
        AccessPolicy.EnsureCanAccessMember(caller, memberId);

        var member = await members.FindAsync(memberId, cancellationToken)
            ?? throw ClubException.NotFound("Member not found.");
        var fitnessClass = await classes.FindAsync(classId, cancellationToken)
            ?? throw ClubException.NotFound("Class not found.");
        var plan = await plans.FindAsync(member.MembershipPlanId, cancellationToken)
            ?? throw ClubException.Conflict("The member's plan no longer exists.");

        var active = (await bookings.ListForMemberAsync(memberId, cancellationToken)).Where(b => b.IsActive).ToList();
        var booked = await bookings.CountActiveForClassAsync(classId, cancellationToken);
        var decision = BookingRules.Evaluate(new BookingContext(fitnessClass, active, booked, plan, clock.GetUtcNow()));
        if (!decision.Allowed)
        {
            throw ClubException.Conflict(decision.Reason ?? "Booking is not allowed.");
        }

        var booking = new ClassBooking
        {
            Id = Guid.NewGuid(),
            MemberId = memberId,
            FitnessClassId = classId,
            Status = BookingStatus.Confirmed,
            BookedAt = clock.GetUtcNow(),
            FitnessClass = fitnessClass,
        };
        await bookings.AddAsync(booking, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToView(booking);
    }

    public async Task CancelAsync(CallerContext caller, Guid bookingId, CancellationToken cancellationToken)
    {
        var booking = await bookings.FindAsync(bookingId, cancellationToken)
            ?? throw ClubException.NotFound("Booking not found.");
        AccessPolicy.EnsureCanAccessMember(caller, booking.MemberId);

        if (!booking.IsActive)
        {
            throw ClubException.Conflict("This booking is already cancelled.");
        }

        booking.Cancel();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BookingView>> ListForMemberAsync(CallerContext caller, Guid memberId, CancellationToken cancellationToken)
    {
        AccessPolicy.EnsureCanAccessMember(caller, memberId);
        var list = await bookings.ListForMemberAsync(memberId, cancellationToken);
        return list.Select(ToView).ToList();
    }

    private static BookingView ToView(ClassBooking booking) => new(
        booking.Id,
        booking.FitnessClassId,
        booking.FitnessClass?.Title ?? string.Empty,
        booking.FitnessClass?.Location ?? string.Empty,
        booking.FitnessClass?.StartsAt ?? default,
        booking.Status);
}
