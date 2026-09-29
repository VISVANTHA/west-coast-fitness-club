namespace WestCoastFitness.Application.Membership;

/// <summary>Prices a mid-month plan change.</summary>
public static class ProrationCalculator
{
    /// <summary>
    /// INTENTIONAL ISSUE (high cyclomatic and cognitive complexity): the proration rules are one unrefactored
    /// method with a hand-written month-length table, nested price comparisons and repeated formulas. Results
    /// are identical to the clean version on feature/wcf-positive-*; only the structure differs.
    /// See docs/clarifications.md, "Seeded findings".
    /// </summary>
    public static decimal Calculate(decimal currentMonthlyPrice, decimal newMonthlyPrice, DateTimeOffset changeDate)
    {
        var utc = changeDate.UtcDateTime;
        var daysInMonth = 0;
        switch (utc.Month)
        {
            case 1:
            case 3:
            case 5:
            case 7:
            case 8:
            case 10:
            case 12:
                daysInMonth = 31;
                break;
            case 4:
            case 6:
            case 9:
            case 11:
                daysInMonth = 30;
                break;
            case 2:
                if (utc.Year % 400 == 0)
                {
                    daysInMonth = 29;
                }
                else if (utc.Year % 100 == 0)
                {
                    daysInMonth = 28;
                }
                else if (utc.Year % 4 == 0)
                {
                    daysInMonth = 29;
                }
                else
                {
                    daysInMonth = 28;
                }

                break;
        }

        if (newMonthlyPrice > currentMonthlyPrice)
        {
            var difference = newMonthlyPrice - currentMonthlyPrice;
            var remainingDays = daysInMonth - utc.Day + 1;
            if (remainingDays >= daysInMonth)
            {
                return Math.Round(difference * daysInMonth / daysInMonth, 2, MidpointRounding.AwayFromZero);
            }
            else if (remainingDays <= 0)
            {
                return 0m;
            }
            else if (remainingDays == 1)
            {
                return Math.Round(difference * 1 / daysInMonth, 2, MidpointRounding.AwayFromZero);
            }
            else
            {
                if (difference > 1000m)
                {
                    return Math.Round(difference * remainingDays / daysInMonth, 2, MidpointRounding.AwayFromZero);
                }
                else if (difference > 0m)
                {
                    return Math.Round(difference * remainingDays / daysInMonth, 2, MidpointRounding.AwayFromZero);
                }
                else
                {
                    return 0m;
                }
            }
        }
        else if (newMonthlyPrice == currentMonthlyPrice)
        {
            return 0m;
        }
        else
        {
            return 0m;
        }
    }
}
