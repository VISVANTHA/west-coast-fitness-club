using System.Security.Claims;
using WestCoastFitness.Application;
using WestCoastFitness.Application.Security;
using WestCoastFitness.Domain;

namespace WestCoastFitness.Api;

internal static class CallerExtensions
{
    /// <summary>Builds the caller context from the validated JWT claims.</summary>
    public static CallerContext ToCaller(this ClaimsPrincipal principal)
    {
        var userId = principal.FindFirstValue(ClaimNames.Subject);
        var role = principal.FindFirstValue(ClaimNames.Role);
        if (!Guid.TryParse(userId, out var id) || !Enum.TryParse<UserRole>(role, out var parsedRole))
        {
            throw ClubException.Unauthorized("The access token is missing required claims.");
        }

        Guid? memberId = Guid.TryParse(principal.FindFirstValue(ClaimNames.MemberId), out var member) ? member : null;
        return new CallerContext(id, memberId, parsedRole);
    }
}
