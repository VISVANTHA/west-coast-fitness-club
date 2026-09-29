using WestCoastFitness.Application;
using WestCoastFitness.Application.Security;
using WestCoastFitness.Domain;

namespace WestCoastFitness.Application.Tests;

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

/// <summary>In-memory data shared by the fake repositories of one test.</summary>
internal sealed class TestClub
{
    public static readonly DateTimeOffset Now = new(2030, 1, 7, 12, 0, 0, TimeSpan.Zero);

    public List<Member> Members { get; } = [];

    public List<MembershipPlan> Plans { get; } = [];

    public List<Trainer> Trainers { get; } = [];

    public List<FitnessClass> Classes { get; } = [];

    public List<ClassBooking> Bookings { get; } = [];

    public List<Payment> Payments { get; } = [];

    public List<UserAccount> Accounts { get; } = [];

    public int SaveCount { get; set; }

    public TimeProvider Clock { get; } = new FixedTimeProvider(Now);

    public MembershipPlan Basic { get; } = new() { Id = Guid.NewGuid(), Name = "Basic", MonthlyPrice = 30m, MaxClassesPerWeek = 2 };

    public MembershipPlan Premium { get; } = new() { Id = Guid.NewGuid(), Name = "Premium", MonthlyPrice = 60m, MaxClassesPerWeek = null };

    public Trainer Trainer { get; } = new() { Id = Guid.NewGuid(), FullName = "Dana Whitfield", Specialty = "Cycling" };

    public TestClub()
    {
        Plans.AddRange([Premium, Basic]);
        Trainers.Add(Trainer);
    }

    public Member AddMember(string first = "Ana", MembershipPlan? plan = null)
    {
        var member = new Member
        {
            Id = Guid.NewGuid(),
            FirstName = first,
            LastName = "Reyes",
            Email = $"{first.ToLowerInvariant()}@example.com",
            MembershipPlanId = (plan ?? Basic).Id,
            JoinedOn = Now.AddDays(-30),
        };
        Members.Add(member);
        return member;
    }

    public FitnessClass AddClass(DateTimeOffset start, int capacity = 10, int minutes = 60)
    {
        var fitnessClass = new FitnessClass
        {
            Id = Guid.NewGuid(),
            Title = $"Class {Classes.Count + 1}",
            TrainerId = Trainer.Id,
            Location = "Phoenix",
            StartsAt = start,
            DurationMinutes = minutes,
            Capacity = capacity,
        };
        Classes.Add(fitnessClass);
        return fitnessClass;
    }

    public ClassBooking AddBooking(Member member, FitnessClass fitnessClass, BookingStatus status = BookingStatus.Confirmed)
    {
        var booking = new ClassBooking
        {
            Id = Guid.NewGuid(),
            MemberId = member.Id,
            FitnessClassId = fitnessClass.Id,
            Status = status,
            FitnessClass = fitnessClass,
            BookedAt = Now,
        };
        Bookings.Add(booking);
        return booking;
    }

    public static CallerContext MemberCaller(Member member) => new(Guid.NewGuid(), member.Id, UserRole.Member);

    public static CallerContext AdminCaller() => new(Guid.NewGuid(), null, UserRole.Administrator);

    public IMemberRepository MemberRepo => new MemberRepo(this);

    public IMembershipPlanRepository PlanRepo => new PlanRepo(this);

    public ITrainerRepository TrainerRepo => new TrainerRepo(this);

    public IFitnessClassRepository ClassRepo => new ClassRepo(this);

    public IBookingRepository BookingRepo => new BookingRepo(this);

    public IPaymentRepository PaymentRepo => new PaymentRepo(this);

    public IUserAccountRepository AccountRepo => new AccountRepo(this);

    public IUnitOfWork UnitOfWork => new FakeUnitOfWork(this);
}

internal sealed class MemberRepo(TestClub club) : IMemberRepository
{
    public Task<Member?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(club.Members.FirstOrDefault(m => m.Id == id));

    public Task<IReadOnlyList<Member>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Member>>(club.Members.ToList());

    public Task AddAsync(Member member, CancellationToken cancellationToken)
    {
        club.Members.Add(member);
        return Task.CompletedTask;
    }
}

internal sealed class PlanRepo(TestClub club) : IMembershipPlanRepository
{
    public Task<MembershipPlan?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(club.Plans.FirstOrDefault(p => p.Id == id));

    public Task<IReadOnlyList<MembershipPlan>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<MembershipPlan>>(club.Plans.ToList());
}

internal sealed class TrainerRepo(TestClub club) : ITrainerRepository
{
    public Task<IReadOnlyList<Trainer>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Trainer>>(club.Trainers.ToList());

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(club.Trainers.Any(t => t.Id == id));
}

internal sealed class ClassRepo(TestClub club) : IFitnessClassRepository
{
    public Task<FitnessClass?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(club.Classes.FirstOrDefault(c => c.Id == id));

    public Task<IReadOnlyList<FitnessClass>> ListUpcomingAsync(DateTimeOffset from, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<FitnessClass>>(club.Classes.Where(c => c.StartsAt > from).ToList());

    public Task AddAsync(FitnessClass fitnessClass, CancellationToken cancellationToken)
    {
        club.Classes.Add(fitnessClass);
        return Task.CompletedTask;
    }
}

internal sealed class BookingRepo(TestClub club) : IBookingRepository
{
    public Task<ClassBooking?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(club.Bookings.FirstOrDefault(b => b.Id == id));

    public Task<IReadOnlyList<ClassBooking>> ListForMemberAsync(Guid memberId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ClassBooking>>(club.Bookings.Where(b => b.MemberId == memberId).ToList());

    public Task<int> CountActiveForClassAsync(Guid fitnessClassId, CancellationToken cancellationToken) =>
        Task.FromResult(club.Bookings.Count(b => b.FitnessClassId == fitnessClassId && b.IsActive));

    public Task<IReadOnlyDictionary<Guid, int>> CountActiveByClassAsync(IReadOnlyCollection<Guid> classIds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<Guid, int>>(
            club.Bookings.Where(b => b.IsActive && classIds.Contains(b.FitnessClassId))
                .GroupBy(b => b.FitnessClassId)
                .ToDictionary(g => g.Key, g => g.Count()));

    public Task AddAsync(ClassBooking booking, CancellationToken cancellationToken)
    {
        club.Bookings.Add(booking);
        return Task.CompletedTask;
    }
}

internal sealed class PaymentRepo(TestClub club) : IPaymentRepository
{
    public Task<IReadOnlyList<Payment>> ListForMemberAsync(Guid memberId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Payment>>(club.Payments.Where(p => p.MemberId == memberId).ToList());

    public Task AddAsync(Payment payment, CancellationToken cancellationToken)
    {
        club.Payments.Add(payment);
        return Task.CompletedTask;
    }
}

internal sealed class AccountRepo(TestClub club) : IUserAccountRepository
{
    public Task<UserAccount?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
        Task.FromResult(club.Accounts.FirstOrDefault(a => a.Email == email));

    public Task AddAsync(UserAccount account, CancellationToken cancellationToken)
    {
        club.Accounts.Add(account);
        return Task.CompletedTask;
    }
}

internal sealed class FakeUnitOfWork(TestClub club) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        club.SaveCount++;
        return Task.CompletedTask;
    }
}

internal sealed class FakeHasher : IPasswordHasher
{
    public string Hash(string password) => "hashed:" + password;

    public bool Verify(string hash, string password) => hash == "hashed:" + password;
}

internal sealed class FakeTokenIssuer : ITokenIssuer
{
    public IssuedToken Issue(UserAccount account) => new("token-for-" + account.Email, TestClub.Now.AddHours(1));
}

internal sealed class FakeGateway : IPaymentGateway
{
    public List<PaymentRequest> Requests { get; } = [];

    public bool Approve { get; set; } = true;

    public Task<PaymentOutcome> ChargeAsync(PaymentRequest request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        return Task.FromResult(new PaymentOutcome(Approve, Approve ? "ref-ok" : "ref-declined", Approve ? null : "declined"));
    }
}
