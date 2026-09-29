using WestCoastFitness.Application.Membership;

namespace WestCoastFitness.Application.Tests;

public class ProrationCalculatorTests
{
    [Theory]
    [InlineData(2030, 1, 1, 30.00, 60.00, 30.00)] // whole month remaining
    [InlineData(2030, 1, 16, 30.00, 60.00, 15.48)] // 16 of 31 days
    [InlineData(2030, 1, 31, 30.00, 60.00, 0.97)] // last day only
    [InlineData(2030, 2, 15, 29.99, 49.99, 10.00)] // 14 of 28 days
    [InlineData(2028, 2, 15, 30.00, 60.00, 15.52)] // leap February, 15 of 29 days
    public void Upgrade_IsChargedForTheRemainingDays(int year, int month, int day, double current, double next, double expected)
    {
        var amount = ProrationCalculator.Calculate((decimal)current, (decimal)next, new DateTimeOffset(year, month, day, 9, 0, 0, TimeSpan.Zero));

        Assert.Equal((decimal)expected, amount);
    }

    [Theory]
    [InlineData(60.00, 30.00)]
    [InlineData(30.00, 30.00)]
    public void DowngradeOrSamePrice_IsFree(double current, double next)
    {
        Assert.Equal(0m, ProrationCalculator.Calculate((decimal)current, (decimal)next, new DateTimeOffset(2030, 1, 10, 0, 0, 0, TimeSpan.Zero)));
    }
}
