namespace WestCoastFitness.Domain;

/// <summary>A payment attempt. Only the gateway reference is kept; card data is never stored.</summary>
public sealed class Payment
{
    public Guid Id { get; set; }

    public Guid MemberId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "USD";

    public string Description { get; set; } = string.Empty;

    public PaymentStatus Status { get; set; }

    public string Reference { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}
