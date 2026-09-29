using Microsoft.EntityFrameworkCore;
using WestCoastFitness.Application;
using WestCoastFitness.Domain;

namespace WestCoastFitness.Infrastructure;

// Every query below is LINQ over EF Core, which always sends values as SQL parameters.
// No repository builds SQL from strings.
internal sealed class MemberRepository(FitnessDbContext db) : IMemberRepository
{
    public Task<Member?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        db.Members.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Member>> ListAsync(CancellationToken cancellationToken) =>
        await db.Members.AsNoTracking().OrderBy(m => m.LastName).ThenBy(m => m.FirstName).ToListAsync(cancellationToken);

    public async Task AddAsync(Member member, CancellationToken cancellationToken) =>
        await db.Members.AddAsync(member, cancellationToken);
}

internal sealed class MembershipPlanRepository(FitnessDbContext db) : IMembershipPlanRepository
{
    public Task<MembershipPlan?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        db.MembershipPlans.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<MembershipPlan>> ListAsync(CancellationToken cancellationToken) =>
        await db.MembershipPlans.AsNoTracking().ToListAsync(cancellationToken);
}

internal sealed class TrainerRepository(FitnessDbContext db) : ITrainerRepository
{
    public async Task<IReadOnlyList<Trainer>> ListAsync(CancellationToken cancellationToken) =>
        await db.Trainers.AsNoTracking().OrderBy(t => t.FullName).ToListAsync(cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken) =>
        db.Trainers.AnyAsync(t => t.Id == id, cancellationToken);
}

internal sealed class FitnessClassRepository(FitnessDbContext db) : IFitnessClassRepository
{
    public Task<FitnessClass?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        db.Classes.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<FitnessClass>> ListUpcomingAsync(DateTimeOffset from, CancellationToken cancellationToken) =>
        await db.Classes.AsNoTracking().Where(c => c.StartsAt > from).OrderBy(c => c.StartsAt).ToListAsync(cancellationToken);

    public async Task AddAsync(FitnessClass fitnessClass, CancellationToken cancellationToken) =>
        await db.Classes.AddAsync(fitnessClass, cancellationToken);
}

internal sealed class BookingRepository(FitnessDbContext db) : IBookingRepository
{
    public Task<ClassBooking?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        db.ClassBookings.Include(b => b.FitnessClass).FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ClassBooking>> ListForMemberAsync(Guid memberId, CancellationToken cancellationToken) =>
        await db.ClassBookings
            .Include(b => b.FitnessClass)
            .Where(b => b.MemberId == memberId)
            .OrderByDescending(b => b.FitnessClass!.StartsAt)
            .ToListAsync(cancellationToken);

    public Task<int> CountActiveForClassAsync(Guid fitnessClassId, CancellationToken cancellationToken) =>
        db.ClassBookings.CountAsync(b => b.FitnessClassId == fitnessClassId && b.Status == BookingStatus.Confirmed, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, int>> CountActiveByClassAsync(IReadOnlyCollection<Guid> classIds, CancellationToken cancellationToken)
    {
        var counts = await db.ClassBookings
            .Where(b => classIds.Contains(b.FitnessClassId) && b.Status == BookingStatus.Confirmed)
            .GroupBy(b => b.FitnessClassId)
            .Select(g => new { ClassId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        return counts.ToDictionary(x => x.ClassId, x => x.Count);
    }

    public async Task AddAsync(ClassBooking booking, CancellationToken cancellationToken) =>
        await db.ClassBookings.AddAsync(booking, cancellationToken);
}

internal sealed class PaymentRepository(FitnessDbContext db) : IPaymentRepository
{
    public async Task<IReadOnlyList<Payment>> ListForMemberAsync(Guid memberId, CancellationToken cancellationToken) =>
        await db.Payments.AsNoTracking()
            .Where(p => p.MemberId == memberId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Payment payment, CancellationToken cancellationToken) =>
        await db.Payments.AddAsync(payment, cancellationToken);
}

internal sealed class UserAccountRepository(FitnessDbContext db) : IUserAccountRepository
{
    public Task<UserAccount?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
        db.UserAccounts.FirstOrDefaultAsync(a => a.Email == email, cancellationToken);

    public async Task AddAsync(UserAccount account, CancellationToken cancellationToken) =>
        await db.UserAccounts.AddAsync(account, cancellationToken);
}

internal sealed class EfUnitOfWork(FitnessDbContext db) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
