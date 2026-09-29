using System.Globalization;
using WestCoastFitness.Domain;

namespace WestCoastFitness.Application.Reporting;

public sealed record ActivityDigest(
    int TotalBookings,
    int ConfirmedBookings,
    int CancelledBookings,
    int UpcomingBookings,
    double CancellationRate,
    string? FavoriteLocation,
    DayOfWeek? BusiestDay,
    double AverageClassMinutes,
    int LongestStreakDays,
    string Headline);

/// <summary>
/// INTENTIONAL ISSUE (duplicated block): MemberActivityReport.cs is the original of this file with only the type names
/// changed, so every helper below exists twice (copy). Both files are also untested (the low-coverage module).
/// See docs/clarifications.md, "Seeded findings".
/// </summary>
public static class MemberActivityDigest
{
    public static ActivityDigest Build(IReadOnlyList<ClassBooking> bookings, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(bookings);

        var confirmed = CountByStatus(bookings, BookingStatus.Confirmed);
        var cancelled = CountByStatus(bookings, BookingStatus.Cancelled);
        var upcoming = CountUpcoming(bookings, now);
        var rate = CancellationRate(confirmed, cancelled);
        var favorite = FavoriteLocation(bookings);
        var busiest = BusiestDay(bookings);
        var average = AverageMinutes(bookings);
        var streak = LongestStreak(bookings);
        var headline = BuildHeadline(confirmed, cancelled, upcoming, favorite, streak);
        return new ActivityDigest(bookings.Count, confirmed, cancelled, upcoming, rate, favorite, busiest, average, streak, headline);
    }

    private static int CountByStatus(IReadOnlyList<ClassBooking> bookings, BookingStatus status)
    {
        var count = 0;
        foreach (var booking in bookings)
        {
            if (booking.Status == status)
            {
                count++;
            }
        }

        return count;
    }

    private static int CountUpcoming(IReadOnlyList<ClassBooking> bookings, DateTimeOffset now)
    {
        var count = 0;
        foreach (var booking in bookings)
        {
            var fitnessClass = booking.FitnessClass;
            if (booking.IsActive && fitnessClass is not null && fitnessClass.StartsAt > now)
            {
                count++;
            }
        }

        return count;
    }

    private static double CancellationRate(int confirmed, int cancelled)
    {
        var total = confirmed + cancelled;
        if (total == 0)
        {
            return 0d;
        }

        var ratio = (double)cancelled / total;
        return Math.Round(ratio * 100d, 1, MidpointRounding.AwayFromZero);
    }

    private static string? FavoriteLocation(IReadOnlyList<ClassBooking> bookings)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var booking in bookings)
        {
            var location = booking.FitnessClass?.Location;
            if (string.IsNullOrWhiteSpace(location))
            {
                continue;
            }

            counts[location] = counts.GetValueOrDefault(location) + 1;
        }

        string? best = null;
        var bestCount = 0;
        foreach (var pair in counts)
        {
            if (pair.Value > bestCount || (pair.Value == bestCount && string.CompareOrdinal(pair.Key, best) < 0))
            {
                best = pair.Key;
                bestCount = pair.Value;
            }
        }

        return best;
    }

    private static DayOfWeek? BusiestDay(IReadOnlyList<ClassBooking> bookings)
    {
        var counts = new int[7];
        var any = false;
        foreach (var booking in bookings)
        {
            var fitnessClass = booking.FitnessClass;
            if (fitnessClass is null)
            {
                continue;
            }

            counts[(int)fitnessClass.StartsAt.UtcDateTime.DayOfWeek]++;
            any = true;
        }

        if (!any)
        {
            return null;
        }

        var bestDay = 0;
        for (var day = 1; day < counts.Length; day++)
        {
            if (counts[day] > counts[bestDay])
            {
                bestDay = day;
            }
        }

        return (DayOfWeek)bestDay;
    }

    private static double AverageMinutes(IReadOnlyList<ClassBooking> bookings)
    {
        var total = 0d;
        var counted = 0;
        foreach (var booking in bookings)
        {
            var fitnessClass = booking.FitnessClass;
            if (fitnessClass is null || !booking.IsActive)
            {
                continue;
            }

            total += fitnessClass.DurationMinutes;
            counted++;
        }

        return counted == 0 ? 0d : Math.Round(total / counted, 1, MidpointRounding.AwayFromZero);
    }

    private static int LongestStreak(IReadOnlyList<ClassBooking> bookings)
    {
        var days = new SortedSet<DateTime>();
        foreach (var booking in bookings)
        {
            if (booking.IsActive && booking.FitnessClass is { } fitnessClass)
            {
                days.Add(fitnessClass.StartsAt.UtcDateTime.Date);
            }
        }

        var longest = 0;
        var current = 0;
        DateTime? previous = null;
        foreach (var day in days)
        {
            current = previous is { } last && (day - last).Days == 1 ? current + 1 : 1;
            longest = Math.Max(longest, current);
            previous = day;
        }

        return longest;
    }

    private static string BuildHeadline(int confirmed, int cancelled, int upcoming, string? favorite, int streak)
    {
        if (confirmed == 0 && cancelled == 0)
        {
            return "No class activity yet.";
        }

        var parts = new List<string>
        {
            string.Create(CultureInfo.InvariantCulture, $"{confirmed} confirmed"),
            string.Create(CultureInfo.InvariantCulture, $"{cancelled} cancelled"),
            string.Create(CultureInfo.InvariantCulture, $"{upcoming} upcoming"),
        };
        if (favorite is not null)
        {
            parts.Add($"favourite club {favorite}");
        }

        if (streak > 1)
        {
            parts.Add(string.Create(CultureInfo.InvariantCulture, $"{streak}-day streak"));
        }

        return string.Join(", ", parts) + ".";
    }

    private static string DescribeTrend(int confirmed, int cancelled)
    {
        if (confirmed > cancelled * 3)
        {
            return "Consistent attendance.";
        }

        if (confirmed > cancelled)
        {
            return "Mostly attending.";
        }

        return confirmed == cancelled ? "Attendance is split evenly." : "Frequently cancelling.";
    }

    private static double MedianLeadTimeDays(IReadOnlyList<ClassBooking> bookings)
    {
        var leads = new List<double>();
        foreach (var booking in bookings)
        {
            var fitnessClass = booking.FitnessClass;
            if (fitnessClass is null)
            {
                continue;
            }

            leads.Add((fitnessClass.StartsAt - booking.BookedAt).TotalDays);
        }

        if (leads.Count == 0)
        {
            return 0d;
        }

        leads.Sort();
        var middle = leads.Count / 2;
        var median = leads.Count % 2 == 1 ? leads[middle] : (leads[middle - 1] + leads[middle]) / 2d;
        return Math.Round(median, 1, MidpointRounding.AwayFromZero);
    }

    private static int CountDistinctLocations(IReadOnlyList<ClassBooking> bookings)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var booking in bookings)
        {
            var location = booking.FitnessClass?.Location;
            if (!string.IsNullOrWhiteSpace(location))
            {
                seen.Add(location);
            }
        }

        return seen.Count;
    }

    public static string Trend(IReadOnlyList<ClassBooking> bookings)
    {
        ArgumentNullException.ThrowIfNull(bookings);
        return DescribeTrend(CountByStatus(bookings, BookingStatus.Confirmed), CountByStatus(bookings, BookingStatus.Cancelled));
    }

    public static (double MedianLeadDays, int DistinctLocations) Reach(IReadOnlyList<ClassBooking> bookings)
    {
        ArgumentNullException.ThrowIfNull(bookings);
        return (MedianLeadTimeDays(bookings), CountDistinctLocations(bookings));
    }
}
