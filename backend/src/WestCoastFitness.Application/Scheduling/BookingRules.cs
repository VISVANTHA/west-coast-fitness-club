using WestCoastFitness.Domain;

namespace WestCoastFitness.Application.Scheduling;

public sealed record BookingContext(
    FitnessClass Target,
    IReadOnlyList<ClassBooking> ActiveBookings,
    int BookedCount,
    MembershipPlan Plan,
    DateTimeOffset Now);

public sealed record BookingDecision(bool Allowed, string? Reason)
{
    public static BookingDecision Allow() => new(true, null);

    public static BookingDecision Deny(string reason) => new(false, reason);
}

/// <summary>Decides whether a member may book a class. Each rule is a small, separately readable check.</summary>
public static class BookingRules
{
    public static BookingDecision Evaluate(BookingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Target.StartsAt <= context.Now)
        {
            return BookingDecision.Deny("This class has already started.");
        }

        if (context.BookedCount >= context.Target.Capacity)
        {
            return BookingDecision.Deny("This class is full.");
        }

        if (context.ActiveBookings.Any(b => b.FitnessClassId == context.Target.Id))
        {
            return BookingDecision.Deny("You are already booked into this class.");
        }

        var conflict = FindOverlap(context);
        if (conflict is not null)
        {
            return BookingDecision.Deny($"This class overlaps with '{conflict.Title}'.");
        }

        return ExceedsWeeklyLimit(context)
            ? BookingDecision.Deny("Your plan's weekly class limit has been reached.")
            : BookingDecision.Allow();
    }

    internal static DateTimeOffset WeekStart(DateTimeOffset value)
    {
        var day = value.UtcDateTime.Date;
        var sinceMonday = ((int)day.DayOfWeek + 6) % 7;
        return new DateTimeOffset(day.AddDays(-sinceMonday), TimeSpan.Zero);
    }

    private static FitnessClass? FindOverlap(BookingContext context) =>
        context.ActiveBookings
            .Select(b => b.FitnessClass)
            .FirstOrDefault(c => c is not null && c.OverlapsWith(context.Target));

    private static bool ExceedsWeeklyLimit(BookingContext context)
    {
        if (context.Plan.MaxClassesPerWeek is not int limit)
        {
            return false;
        }

        var weekStart = WeekStart(context.Target.StartsAt);
        var weekEnd = weekStart.AddDays(7);
        var used = context.ActiveBookings.Count(b =>
            b.FitnessClass is { } c && c.StartsAt >= weekStart && c.StartsAt < weekEnd);
        return used >= limit;
    }
}
