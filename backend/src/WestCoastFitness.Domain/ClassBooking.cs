namespace WestCoastFitness.Domain;

public sealed class ClassBooking
{
    public Guid Id { get; set; }

    public Guid MemberId { get; set; }

    public Guid FitnessClassId { get; set; }

    public BookingStatus Status { get; set; }

    public DateTimeOffset BookedAt { get; set; }

    public FitnessClass? FitnessClass { get; set; }

    public bool IsActive => Status == BookingStatus.Confirmed;

    public void Cancel()
    {
        Status = BookingStatus.Cancelled;
    }
}
