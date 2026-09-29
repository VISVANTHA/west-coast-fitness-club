namespace WestCoastFitness.Domain;

public sealed class FitnessClass
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public Guid TrainerId { get; set; }

    /// <summary>Gets or sets the Arizona club location, for example Phoenix or Tucson.</summary>
    public string Location { get; set; } = string.Empty;

    public DateTimeOffset StartsAt { get; set; }

    public int DurationMinutes { get; set; }

    public int Capacity { get; set; }

    public DateTimeOffset EndsAt => StartsAt.AddMinutes(DurationMinutes);

    public bool OverlapsWith(FitnessClass other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return StartsAt < other.EndsAt && other.StartsAt < EndsAt;
    }
}
