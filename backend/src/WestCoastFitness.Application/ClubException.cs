namespace WestCoastFitness.Application;

public enum ClubErrorKind
{
    Validation,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict,
    PaymentDeclined,
}

/// <summary>A business-rule failure that the API maps to an HTTP problem response.</summary>
public sealed class ClubException : Exception
{
    public ClubException(ClubErrorKind kind, string message)
        : base(message)
    {
        Kind = kind;
    }

    public ClubErrorKind Kind { get; }

    public static ClubException Validation(string message) => new(ClubErrorKind.Validation, message);

    public static ClubException Unauthorized(string message) => new(ClubErrorKind.Unauthorized, message);

    public static ClubException Forbidden(string message) => new(ClubErrorKind.Forbidden, message);

    public static ClubException NotFound(string message) => new(ClubErrorKind.NotFound, message);

    public static ClubException Conflict(string message) => new(ClubErrorKind.Conflict, message);

    public static ClubException PaymentDeclined(string message) => new(ClubErrorKind.PaymentDeclined, message);
}
