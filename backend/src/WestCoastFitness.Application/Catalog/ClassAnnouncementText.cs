using WestCoastFitness.Domain;

namespace WestCoastFitness.Application.Catalog;

/// <summary>
/// Member-facing class announcement.
/// NEAR-DUPLICATE (intentional, documented): <see cref="StaffClassAnnouncementText"/> repeats the
/// duration helper below verbatim. It is the one minor clone the positive branches leave for jscpd.
/// </summary>
public static class ClassAnnouncementText
{
    public static string Describe(FitnessClass fitnessClass, string trainerName)
    {
        ArgumentNullException.ThrowIfNull(fitnessClass);
        return $"Join {trainerName} for {fitnessClass.Title} at our {fitnessClass.Location} club: {DescribeWindow(fitnessClass)}.";
    }

    private static string DescribeWindow(FitnessClass fitnessClass)
    {
        var start = fitnessClass.StartsAt.UtcDateTime;
        var end = fitnessClass.EndsAt.UtcDateTime;
        var sameDay = start.Date == end.Date;
        var hours = fitnessClass.DurationMinutes / 60;
        var minutes = fitnessClass.DurationMinutes % 60;
        var length = hours > 0 && minutes > 0
            ? $"{hours}h {minutes}m"
            : hours > 0
                ? $"{hours}h"
                : $"{minutes}m";
        var startText = start.ToString("ddd MMM d, HH:mm", System.Globalization.CultureInfo.InvariantCulture);
        var endText = sameDay
            ? end.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture)
            : end.ToString("ddd MMM d, HH:mm", System.Globalization.CultureInfo.InvariantCulture);
        return $"{startText} to {endText} UTC ({length})";
    }
}
