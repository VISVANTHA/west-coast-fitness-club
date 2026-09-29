namespace WestCoastFitness.Domain;

public sealed class Trainer
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Specialty { get; set; } = string.Empty;
}
