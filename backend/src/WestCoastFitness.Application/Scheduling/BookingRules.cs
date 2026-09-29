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

/// <summary>Decides whether a member may book a class.</summary>
public static class BookingRules
{
    /// <summary>
    /// INTENTIONAL ISSUE (high cyclomatic and cognitive complexity): the booking-conflict logic is kept as one
    /// unrefactored method with nested branches, an inline week calculation and repeated decision points. The
    /// behaviour is identical to the clean version on feature/wcf-positive-*; only the structure differs.
    /// See docs/clarifications.md, "Seeded findings".
    /// </summary>
    public static BookingDecision Evaluate(BookingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var target = context.Target;
        if (target.StartsAt <= context.Now)
        {
            return BookingDecision.Deny("This class has already started.");
        }
        else
        {
            if (context.BookedCount >= target.Capacity)
            {
                return BookingDecision.Deny("This class is full.");
            }
            else
            {
                var day = target.StartsAt.UtcDateTime.Date;
                var sinceMonday = 0;
                switch (day.DayOfWeek)
                {
                    case DayOfWeek.Monday:
                        sinceMonday = 0;
                        break;
                    case DayOfWeek.Tuesday:
                        sinceMonday = 1;
                        break;
                    case DayOfWeek.Wednesday:
                        sinceMonday = 2;
                        break;
                    case DayOfWeek.Thursday:
                        sinceMonday = 3;
                        break;
                    case DayOfWeek.Friday:
                        sinceMonday = 4;
                        break;
                    case DayOfWeek.Saturday:
                        sinceMonday = 5;
                        break;
                    case DayOfWeek.Sunday:
                        sinceMonday = 6;
                        break;
                }

                var weekStart = new DateTimeOffset(day.AddDays(-sinceMonday), TimeSpan.Zero);
                var weekEnd = weekStart.AddDays(7);
                var alreadyBooked = false;
                string? overlapTitle = null;
                var usedThisWeek = 0;

                foreach (var booking in context.ActiveBookings)
                {
                    if (booking.FitnessClassId == target.Id)
                    {
                        alreadyBooked = true;
                    }

                    var other = booking.FitnessClass;
                    if (other is not null)
                    {
                        if (overlapTitle is null)
                        {
                            if (other.StartsAt < target.EndsAt && target.StartsAt < other.EndsAt)
                            {
                                overlapTitle = other.Title;
                            }
                        }

                        if (other.StartsAt >= weekStart && other.StartsAt < weekEnd)
                        {
                            usedThisWeek++;
                        }
                    }
                }

                if (alreadyBooked)
                {
                    return BookingDecision.Deny("You are already booked into this class.");
                }
                else if (overlapTitle is not null)
                {
                    return BookingDecision.Deny($"This class overlaps with '{overlapTitle}'.");
                }
                else if (context.Plan.MaxClassesPerWeek is int limit)
                {
                    if (limit <= 0)
                    {
                        return BookingDecision.Deny("Your plan's weekly class limit has been reached.");
                    }
                    else if (limit == 1 && usedThisWeek >= 1)
                    {
                        return BookingDecision.Deny("Your plan's weekly class limit has been reached.");
                    }
                    else if (usedThisWeek >= limit)
                    {
                        return BookingDecision.Deny("Your plan's weekly class limit has been reached.");
                    }
                    else
                    {
                        return BookingDecision.Allow();
                    }
                }
                else
                {
                    return BookingDecision.Allow();
                }
            }
        }
    }

    internal static DateTimeOffset WeekStart(DateTimeOffset value)
    {
        var day = value.UtcDateTime.Date;
        var sinceMonday = ((int)day.DayOfWeek + 6) % 7;
        return new DateTimeOffset(day.AddDays(-sinceMonday), TimeSpan.Zero);
    }
}
