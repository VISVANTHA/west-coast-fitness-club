using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using WestCoastFitness.Application;
using WestCoastFitness.Domain;

namespace WestCoastFitness.Api.Tests;

public class PipelineUnitTests
{
    private static async Task<(int Status, string Body)> RunAsync(RequestDelegate next)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        await new ClubExceptionMiddleware(next, NullLogger<ClubExceptionMiddleware>.Instance).InvokeAsync(context);
        context.Response.Body.Position = 0;
        return (context.Response.StatusCode, await new StreamReader(context.Response.Body).ReadToEndAsync());
    }

    [Theory]
    [InlineData(ClubErrorKind.Validation, 400)]
    [InlineData(ClubErrorKind.Unauthorized, 401)]
    [InlineData(ClubErrorKind.PaymentDeclined, 402)]
    [InlineData(ClubErrorKind.Forbidden, 403)]
    [InlineData(ClubErrorKind.NotFound, 404)]
    [InlineData(ClubErrorKind.Conflict, 409)]
    public async Task ClubExceptions_MapToTheirStatusCodes(ClubErrorKind kind, int expected)
    {
        var (status, body) = await RunAsync(_ => throw new ClubException(kind, "business rule"));

        Assert.Equal(expected, status);
        Assert.Contains("business rule", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DatabaseConstraintViolations_BecomeConflicts()
    {
        var (status, body) = await RunAsync(_ => throw new DbUpdateException("duplicate key value violates unique constraint"));

        Assert.Equal(409, status);
        Assert.DoesNotContain("duplicate key", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnexpectedErrors_AreHiddenBehindAGenericMessage()
    {
        var (status, body) = await RunAsync(_ => throw new InvalidOperationException("secret internal detail"));

        Assert.Equal(500, status);
        Assert.DoesNotContain("secret internal detail", body, StringComparison.Ordinal);
        Assert.Contains("unexpected error", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Cancellation_IsNotSwallowed()
    {
        await Assert.ThrowsAsync<OperationCanceledException>(() => RunAsync(_ => throw new OperationCanceledException()));
    }

    [Fact]
    public async Task SuccessfulRequests_PassThrough()
    {
        var (status, _) = await RunAsync(context =>
        {
            context.Response.StatusCode = 204;
            return Task.CompletedTask;
        });

        Assert.Equal(204, status);
    }

    [Fact]
    public void TokenIssuer_WritesTheExpectedClaims()
    {
        var options = Options.Create(new JwtOptions
        {
            Issuer = "issuer",
            Audience = "audience",
            SigningKey = ApiFactory.SigningKey,
            ExpiryMinutes = 15,
        });
        var member = new UserAccount { Id = Guid.NewGuid(), Email = "ana@example.com", Role = UserRole.Member, MemberId = Guid.NewGuid() };
        var admin = new UserAccount { Id = Guid.NewGuid(), Email = "admin@example.com", Role = UserRole.Administrator };
        var issuer = new JwtTokenIssuer(options, TimeProvider.System);

        var memberToken = new JsonWebTokenHandler().ReadJsonWebToken(issuer.Issue(member).Token);
        var adminToken = new JsonWebTokenHandler().ReadJsonWebToken(issuer.Issue(admin).Token);

        Assert.Equal(member.Id.ToString(), memberToken.GetClaim(ClaimNames.Subject).Value);
        Assert.Equal("Member", memberToken.GetClaim(ClaimNames.Role).Value);
        Assert.Equal(member.MemberId.ToString(), memberToken.GetClaim(ClaimNames.MemberId).Value);
        Assert.False(adminToken.TryGetClaim(ClaimNames.MemberId, out _));
        Assert.Equal("issuer", memberToken.Issuer);
        Assert.Throws<ArgumentNullException>(() => issuer.Issue(null!));
    }

    [Fact]
    public void Caller_IsBuiltFromClaims()
    {
        var memberId = Guid.NewGuid();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimNames.Subject, Guid.NewGuid().ToString()),
            new Claim(ClaimNames.Role, "Member"),
            new Claim(ClaimNames.MemberId, memberId.ToString()),
        ]));

        var caller = principal.ToCaller();

        Assert.Equal(memberId, caller.MemberId);
        Assert.Equal(UserRole.Member, caller.Role);
    }

    [Fact]
    public void Caller_WithoutClaimsIsUnauthorized()
    {
        var ex = Assert.Throws<ClubException>(() => new ClaimsPrincipal(new ClaimsIdentity()).ToCaller());

        Assert.Equal(ClubErrorKind.Unauthorized, ex.Kind);
    }
}
