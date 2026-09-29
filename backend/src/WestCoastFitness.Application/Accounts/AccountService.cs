using System.Net.Mail;
using WestCoastFitness.Domain;

namespace WestCoastFitness.Application.Accounts;

public sealed record RegisterRequest(string Email, string Password, string FirstName, string LastName, string Phone);

public sealed record LoginRequest(string Email, string Password);

public sealed record AuthResult(string Token, DateTimeOffset ExpiresAt, Guid UserId, Guid? MemberId, UserRole Role);

public sealed class AccountService(
    IUserAccountRepository accounts,
    IMemberRepository members,
    IMembershipPlanRepository plans,
    IPasswordHasher hasher,
    ITokenIssuer tokens,
    IUnitOfWork unitOfWork,
    TimeProvider clock)
{
    internal const int MinimumPasswordLength = 12;

    public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validate(request);

        var email = request.Email.Trim().ToLowerInvariant();
        if (await accounts.FindByEmailAsync(email, cancellationToken) is not null)
        {
            throw ClubException.Conflict("An account with this email already exists.");
        }

        var entryPlan = (await plans.ListAsync(cancellationToken)).OrderBy(p => p.MonthlyPrice).FirstOrDefault()
            ?? throw ClubException.Conflict("No membership plans are available yet.");

        var member = new Member
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            Phone = request.Phone.Trim(),
            MembershipPlanId = entryPlan.Id,
            JoinedOn = clock.GetUtcNow(),
        };
        var account = new UserAccount
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = hasher.Hash(request.Password),
            Role = UserRole.Member,
            MemberId = member.Id,
        };

        await members.AddAsync(member, cancellationToken);
        await accounts.AddAsync(account, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToResult(account);
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var account = await accounts.FindByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (account is null || !hasher.Verify(account.PasswordHash, request.Password))
        {
            throw ClubException.Unauthorized("Email or password is incorrect.");
        }

        return ToResult(account);
    }

    private static void Validate(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
        {
            throw ClubException.Validation("First and last name are required.");
        }

        if (!MailAddress.TryCreate(request.Email, out _))
        {
            throw ClubException.Validation("A valid email address is required.");
        }

        if (request.Password is null || request.Password.Length < MinimumPasswordLength)
        {
            throw ClubException.Validation($"Password must be at least {MinimumPasswordLength} characters.");
        }
    }

    private AuthResult ToResult(UserAccount account)
    {
        var issued = tokens.Issue(account);
        return new AuthResult(issued.Token, issued.ExpiresAt, account.Id, account.MemberId, account.Role);
    }
}
