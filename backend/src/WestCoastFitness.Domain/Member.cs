namespace WestCoastFitness.Domain;

public sealed class Member
{
    public Guid Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public Guid MembershipPlanId { get; set; }

    public DateTimeOffset JoinedOn { get; set; }

    public string FullName => $"{FirstName} {LastName}";
}
