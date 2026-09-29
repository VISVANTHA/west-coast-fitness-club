using WestCoastFitness.Domain;

namespace WestCoastFitness.Application.Security;

/// <summary>The authenticated caller, resolved from the JWT by the API layer.</summary>
public sealed record CallerContext(Guid UserId, Guid? MemberId, UserRole Role);
