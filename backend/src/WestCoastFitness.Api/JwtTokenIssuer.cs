using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using WestCoastFitness.Application;
using WestCoastFitness.Domain;

namespace WestCoastFitness.Api;

public sealed class JwtOptions
{
    public const int MinimumKeyLength = 32;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    /// <summary>Gets or sets the HMAC key. Supplied by user-secrets or the Jwt__SigningKey environment variable.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public int ExpiryMinutes { get; set; } = 60;
}

public static class ClaimNames
{
    public const string Subject = "sub";
    public const string Email = "email";
    public const string Role = "role";
    public const string MemberId = "member_id";
}

public sealed class JwtTokenIssuer(IOptions<JwtOptions> options, TimeProvider clock) : ITokenIssuer
{
    public IssuedToken Issue(UserAccount account)
    {
        ArgumentNullException.ThrowIfNull(account);
        var settings = options.Value;
        var expires = clock.GetUtcNow().AddMinutes(settings.ExpiryMinutes);

        var claims = new Dictionary<string, object>
        {
            [ClaimNames.Subject] = account.Id.ToString(),
            [ClaimNames.Email] = account.Email,
            [ClaimNames.Role] = account.Role.ToString(),
        };
        if (account.MemberId is { } memberId)
        {
            claims[ClaimNames.MemberId] = memberId.ToString();
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            Expires = expires.UtcDateTime,
            Claims = claims,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)),
                SecurityAlgorithms.HmacSha256),
        };
        return new IssuedToken(new JsonWebTokenHandler().CreateToken(descriptor), expires);
    }
}
