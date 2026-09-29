using System.Globalization;
using Microsoft.AspNetCore.Identity;
using WestCoastFitness.Application;
using WestCoastFitness.Domain;

namespace WestCoastFitness.Infrastructure;

/// <summary>PBKDF2 password hashing from Microsoft.Extensions.Identity.Core (hashing only, no Identity stores).</summary>
public sealed class IdentityPasswordHasher : Application.IPasswordHasher
{
    private readonly PasswordHasher<UserAccount> _hasher = new();
    private readonly UserAccount _subject = new();

    public string Hash(string password) => _hasher.HashPassword(_subject, password);

    public bool Verify(string hash, string password) =>
        _hasher.VerifyHashedPassword(_subject, hash, password) != PasswordVerificationResult.Failed;
}

/// <summary>
/// In-process stand-in for a card processor. It declines zero and negative amounts, refuses raw card
/// numbers, keeps no state, and never stores card data: only an opaque token passes through it.
/// </summary>
public sealed class MockPaymentGateway : IPaymentGateway
{
    public const string DeclineToken = "tok_decline";

    public Task<PaymentOutcome> ChargeAsync(PaymentRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Amount <= 0m)
        {
            return Task.FromResult(Decline("Amount must be greater than zero."));
        }

        if (string.IsNullOrWhiteSpace(request.PaymentToken))
        {
            return Task.FromResult(Decline("A payment token is required."));
        }

        if (LooksLikeCardNumber(request.PaymentToken))
        {
            return Task.FromResult(Decline("Raw card numbers are not accepted; supply a payment token."));
        }

        if (string.Equals(request.PaymentToken, DeclineToken, StringComparison.Ordinal))
        {
            return Task.FromResult(Decline("The card was declined."));
        }

        var reference = string.Create(CultureInfo.InvariantCulture, $"mock-{Guid.NewGuid():N}");
        return Task.FromResult(new PaymentOutcome(true, reference, null));
    }

    private static PaymentOutcome Decline(string reason) =>
        new(false, string.Create(CultureInfo.InvariantCulture, $"declined-{Guid.NewGuid():N}"), reason);

    private static bool LooksLikeCardNumber(string token)
    {
        var digits = token.Where(char.IsDigit).Count();
        return digits >= 13 && token.All(c => char.IsDigit(c) || c is ' ' or '-');
    }
}
