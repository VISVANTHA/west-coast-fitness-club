using WestCoastFitness.Domain;

namespace WestCoastFitness.Application.Security;

/// <summary>Object-level authorization: a member may only reach their own records.</summary>
public static class AccessPolicy
{
    public static void EnsureCanAccessMember(CallerContext caller, Guid memberId)
    {
        ArgumentNullException.ThrowIfNull(caller);
        if (caller.Role == UserRole.Administrator || caller.MemberId == memberId)
        {
            return;
        }

        throw ClubException.Forbidden("You do not have access to this member's records.");
    }

    public static void EnsureAdministrator(CallerContext caller)
    {
        ArgumentNullException.ThrowIfNull(caller);
        if (caller.Role != UserRole.Administrator)
        {
            throw ClubException.Forbidden("Administrator access is required.");
        }
    }
}
