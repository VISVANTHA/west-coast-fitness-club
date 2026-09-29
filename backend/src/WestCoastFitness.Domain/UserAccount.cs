namespace WestCoastFitness.Domain;

public sealed class UserAccount
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    /// <summary>Gets or sets the owned member profile; null for administrators.</summary>
    public Guid? MemberId { get; set; }
}
