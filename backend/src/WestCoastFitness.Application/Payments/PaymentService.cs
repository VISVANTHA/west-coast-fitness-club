using WestCoastFitness.Application.Security;
using WestCoastFitness.Domain;

namespace WestCoastFitness.Application.Payments;

public sealed record ChargeRequest(decimal Amount, string PaymentToken, string Description);

public sealed record PaymentView(Guid Id, decimal Amount, string Currency, string Description, PaymentStatus Status, string Reference, DateTimeOffset CreatedAt);

public sealed class PaymentService(
    IMemberRepository members,
    IPaymentRepository payments,
    IPaymentGateway gateway,
    IUnitOfWork unitOfWork,
    TimeProvider clock)
{
    private const string Currency = "USD";

    public async Task<PaymentView> ChargeAsync(CallerContext caller, Guid memberId, ChargeRequest request, CancellationToken cancellationToken)
    {
        AccessPolicy.EnsureCanAccessMember(caller, memberId);
        ArgumentNullException.ThrowIfNull(request);
        if (await members.FindAsync(memberId, cancellationToken) is null)
        {
            throw ClubException.NotFound("Member not found.");
        }

        return ToView(await ProcessAsync(memberId, request, cancellationToken));
    }

    public async Task<IReadOnlyList<PaymentView>> ListForMemberAsync(CallerContext caller, Guid memberId, CancellationToken cancellationToken)
    {
        AccessPolicy.EnsureCanAccessMember(caller, memberId);
        var list = await payments.ListForMemberAsync(memberId, cancellationToken);
        return list.Select(ToView).ToList();
    }

    /// <summary>Charges through the gateway and records the attempt. Declined attempts are recorded too.</summary>
    public async Task<Payment> ProcessAsync(Guid memberId, ChargeRequest request, CancellationToken cancellationToken)
    {
        var outcome = await gateway.ChargeAsync(
            new PaymentRequest(request.Amount, Currency, request.PaymentToken, request.Description),
            cancellationToken);

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            MemberId = memberId,
            Amount = request.Amount,
            Currency = Currency,
            Description = request.Description,
            Status = outcome.Approved ? PaymentStatus.Approved : PaymentStatus.Declined,
            Reference = outcome.Reference,
            CreatedAt = clock.GetUtcNow(),
        };
        await payments.AddAsync(payment, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return payment;
    }

    private static PaymentView ToView(Payment payment) =>
        new(payment.Id, payment.Amount, payment.Currency, payment.Description, payment.Status, payment.Reference, payment.CreatedAt);
}
