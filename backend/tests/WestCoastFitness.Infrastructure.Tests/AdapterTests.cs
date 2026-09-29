using WestCoastFitness.Application;
using WestCoastFitness.Infrastructure;

namespace WestCoastFitness.Infrastructure.Tests;

public class AdapterTests
{
    private static readonly MockPaymentGateway Gateway = new();

    private static PaymentRequest Charge(decimal amount, string token = "tok_visa") => new(amount, "USD", token, "Membership");

    [Fact]
    public void PasswordHasher_VerifiesTheRightPasswordOnly()
    {
        var hasher = new IdentityPasswordHasher();

        var hash = hasher.Hash("correct-horse-battery");

        Assert.NotEqual("correct-horse-battery", hash);
        Assert.True(hasher.Verify(hash, "correct-horse-battery"));
        Assert.False(hasher.Verify(hash, "wrong-horse-battery"));
        Assert.NotEqual(hash, hasher.Hash("correct-horse-battery"));
    }

    [Fact]
    public async Task Gateway_ApprovesAPositiveAmount()
    {
        var outcome = await Gateway.ChargeAsync(Charge(49.99m), CancellationToken.None);

        Assert.True(outcome.Approved);
        Assert.StartsWith("mock-", outcome.Reference, StringComparison.Ordinal);
        Assert.Null(outcome.DeclineReason);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(-0.01)]
    public async Task Gateway_DeclinesZeroAndNegativeAmounts(double amount)
    {
        var outcome = await Gateway.ChargeAsync(Charge((decimal)amount), CancellationToken.None);

        Assert.False(outcome.Approved);
        Assert.StartsWith("declined-", outcome.Reference, StringComparison.Ordinal);
        Assert.Contains("greater than zero", outcome.DeclineReason);
    }

    [Theory]
    [InlineData("4111111111111111")]
    [InlineData("4111 1111 1111 1111")]
    [InlineData("4111-1111-1111-1111")]
    public async Task Gateway_RefusesRawCardNumbers(string cardNumber)
    {
        var outcome = await Gateway.ChargeAsync(Charge(10m, cardNumber), CancellationToken.None);

        Assert.False(outcome.Approved);
        Assert.Contains("Raw card numbers", outcome.DeclineReason);
        Assert.DoesNotContain(cardNumber, outcome.Reference, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Gateway_RequiresAToken(string token)
    {
        var outcome = await Gateway.ChargeAsync(Charge(10m, token), CancellationToken.None);

        Assert.False(outcome.Approved);
        Assert.Contains("token is required", outcome.DeclineReason);
    }

    [Fact]
    public async Task Gateway_DeclinesTheTestDeclineToken()
    {
        var outcome = await Gateway.ChargeAsync(Charge(10m, MockPaymentGateway.DeclineToken), CancellationToken.None);

        Assert.False(outcome.Approved);
        Assert.Equal("The card was declined.", outcome.DeclineReason);
    }

    [Fact]
    public async Task Gateway_NeverEchoesTheTokenInTheReference()
    {
        var outcome = await Gateway.ChargeAsync(Charge(10m, "tok_secret_abc"), CancellationToken.None);

        Assert.DoesNotContain("secret", outcome.Reference, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Gateway_NullRequestThrows()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => Gateway.ChargeAsync(null!, CancellationToken.None));
    }
}
