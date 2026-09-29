namespace WestCoastFitness.Domain;

public sealed class MembershipPlan
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal MonthlyPrice { get; set; }

    /// <summary>Gets or sets the weekly class allowance; null means unlimited.</summary>
    public int? MaxClassesPerWeek { get; set; }
}
