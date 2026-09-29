namespace WestCoastFitness.Application.Membership;

/// <summary>Prices a mid-month plan change.</summary>
public static class ProrationCalculator
{
    /// <summary>
    /// Returns the amount due when moving to a more expensive plan on the given day. The change day
    /// counts as a full day on the new plan. Downgrades are free and never produce a refund.
    /// </summary>
    public static decimal Calculate(decimal currentMonthlyPrice, decimal newMonthlyPrice, DateTimeOffset changeDate)
    {
        var difference = newMonthlyPrice - currentMonthlyPrice;
        if (difference <= 0m)
        {
            return 0m;
        }

        var utc = changeDate.UtcDateTime;
        var daysInMonth = DateTime.DaysInMonth(utc.Year, utc.Month);
        var remainingDays = daysInMonth - utc.Day + 1;
        return Math.Round(difference * remainingDays / daysInMonth, 2, MidpointRounding.AwayFromZero);
    }
}
