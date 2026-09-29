using WestCoastFitness.Domain;

namespace WestCoastFitness.Application;

public interface IMemberRepository
{
    Task<Member?> FindAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Member>> ListAsync(CancellationToken cancellationToken);

    Task AddAsync(Member member, CancellationToken cancellationToken);
}

public interface IMembershipPlanRepository
{
    Task<MembershipPlan?> FindAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<MembershipPlan>> ListAsync(CancellationToken cancellationToken);
}

public interface ITrainerRepository
{
    Task<IReadOnlyList<Trainer>> ListAsync(CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);
}

public interface IFitnessClassRepository
{
    Task<FitnessClass?> FindAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<FitnessClass>> ListUpcomingAsync(DateTimeOffset from, CancellationToken cancellationToken);

    Task AddAsync(FitnessClass fitnessClass, CancellationToken cancellationToken);
}

public interface IBookingRepository
{
    Task<ClassBooking?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Lists a member's bookings, newest class first, with the class loaded.</summary>
    Task<IReadOnlyList<ClassBooking>> ListForMemberAsync(Guid memberId, CancellationToken cancellationToken);

    Task<int> CountActiveForClassAsync(Guid fitnessClassId, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, int>> CountActiveByClassAsync(IReadOnlyCollection<Guid> classIds, CancellationToken cancellationToken);

    Task AddAsync(ClassBooking booking, CancellationToken cancellationToken);
}

public interface IPaymentRepository
{
    Task<IReadOnlyList<Payment>> ListForMemberAsync(Guid memberId, CancellationToken cancellationToken);

    Task AddAsync(Payment payment, CancellationToken cancellationToken);
}

public interface IUserAccountRepository
{
    Task<UserAccount?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    Task AddAsync(UserAccount account, CancellationToken cancellationToken);
}

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string hash, string password);
}

public sealed record IssuedToken(string Token, DateTimeOffset ExpiresAt);

public interface ITokenIssuer
{
    IssuedToken Issue(UserAccount account);
}

/// <summary>A charge request. The token is an opaque client-side payment token, never a card number.</summary>
public sealed record PaymentRequest(decimal Amount, string Currency, string PaymentToken, string Description);

public sealed record PaymentOutcome(bool Approved, string Reference, string? DeclineReason);

public interface IPaymentGateway
{
    Task<PaymentOutcome> ChargeAsync(PaymentRequest request, CancellationToken cancellationToken);
}
