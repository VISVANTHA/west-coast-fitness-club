using WestCoastFitness.Domain;

namespace WestCoastFitness.Application.Catalog;

/// <summary>
/// Staff-facing class announcement.
/// NEAR-DUPLICATE (intentional, documented): the duration helper below matches
/// <see cref="ClassAnnouncementText"/>; only the sentence wrapped around it differs.
/// </summary>
public static class StaffClassAnnouncementText
{
    public static string Describe(FitnessClass fitnessClass, string trainerName, int spotsLeft)
    {
        ArgumentNullException.ThrowIfNull(fitnessClass);
        return $"[{fitnessClass.Location}] {fitnessClass.Title} led by {trainerName}, {spotsLeft} of {fitnessClass.Capacity} spots open, {DescribeWindow(fitnessClass)}.";
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
