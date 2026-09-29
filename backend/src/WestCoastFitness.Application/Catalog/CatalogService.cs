using WestCoastFitness.Application.Security;
using WestCoastFitness.Domain;

namespace WestCoastFitness.Application.Catalog;

public sealed record ClassSummary(
    Guid Id,
    string Title,
    string TrainerName,
    string Location,
    DateTimeOffset StartsAt,
    int DurationMinutes,
    int Capacity,
    int SpotsLeft,
    string Announcement);

public sealed record CreateClassRequest(string Title, Guid TrainerId, string Location, DateTimeOffset StartsAt, int DurationMinutes, int Capacity);

public sealed class CatalogService(
    IMembershipPlanRepository plans,
    ITrainerRepository trainers,
    IFitnessClassRepository classes,
    IBookingRepository bookings,
    IUnitOfWork unitOfWork,
    TimeProvider clock)
{
    public async Task<IReadOnlyList<MembershipPlan>> ListPlansAsync(CancellationToken cancellationToken) =>
        (await plans.ListAsync(cancellationToken)).OrderBy(p => p.MonthlyPrice).ToList();

    public Task<IReadOnlyList<Trainer>> ListTrainersAsync(CancellationToken cancellationToken) =>
        trainers.ListAsync(cancellationToken);

    public async Task<IReadOnlyList<ClassSummary>> ListUpcomingClassesAsync(CancellationToken cancellationToken)
    {
        var (upcoming, booked, trainerNames) = await LoadRosterAsync(cancellationToken);

        return upcoming
            .OrderBy(c => c.StartsAt)
            .Select(c =>
            {
                var trainerName = trainerNames.GetValueOrDefault(c.TrainerId, "TBA");
                return new ClassSummary(
                    c.Id,
                    c.Title,
                    trainerName,
                    c.Location,
                    c.StartsAt,
                    c.DurationMinutes,
                    c.Capacity,
                    Math.Max(0, c.Capacity - booked.GetValueOrDefault(c.Id)),
                    ClassAnnouncementText.Describe(c, trainerName));
            })
            .ToList();
    }

    /// <summary>Staff-only roster lines showing trainer and remaining spots for upcoming classes.</summary>
    public async Task<IReadOnlyList<string>> ListStaffAnnouncementsAsync(CallerContext caller, CancellationToken cancellationToken)
    {
        AccessPolicy.EnsureAdministrator(caller);
        var (upcoming, booked, trainerNames) = await LoadRosterAsync(cancellationToken);

        return upcoming
            .OrderBy(c => c.StartsAt)
            .Select(c => StaffClassAnnouncementText.Describe(
                c,
                trainerNames.GetValueOrDefault(c.TrainerId, "TBA"),
                Math.Max(0, c.Capacity - booked.GetValueOrDefault(c.Id))))
            .ToList();
    }

    private async Task<(IReadOnlyList<FitnessClass> Upcoming, IReadOnlyDictionary<Guid, int> Booked, Dictionary<Guid, string> TrainerNames)> LoadRosterAsync(
        CancellationToken cancellationToken)
    {
        var upcoming = await classes.ListUpcomingAsync(clock.GetUtcNow(), cancellationToken);
        var booked = await bookings.CountActiveByClassAsync(upcoming.Select(c => c.Id).ToList(), cancellationToken);
        var trainerNames = (await trainers.ListAsync(cancellationToken)).ToDictionary(t => t.Id, t => t.FullName);
        return (upcoming, booked, trainerNames);
    }

    public async Task<FitnessClass> CreateClassAsync(CallerContext caller, CreateClassRequest request, CancellationToken cancellationToken)
    {
        AccessPolicy.EnsureAdministrator(caller);
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Location))
        {
            throw ClubException.Validation("Title and location are required.");
        }

        if (request.Capacity is < 1 or > 200 || request.DurationMinutes is < 15 or > 240)
        {
            throw ClubException.Validation("Capacity must be 1-200 and duration 15-240 minutes.");
        }

        if (request.StartsAt <= clock.GetUtcNow())
        {
            throw ClubException.Validation("A class must start in the future.");
        }

        if (!await trainers.ExistsAsync(request.TrainerId, cancellationToken))
        {
            throw ClubException.NotFound("Trainer not found.");
        }

        var fitnessClass = new FitnessClass
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            TrainerId = request.TrainerId,
            Location = request.Location.Trim(),
            StartsAt = request.StartsAt.ToUniversalTime(),
            DurationMinutes = request.DurationMinutes,
            Capacity = request.Capacity,
        };
        await classes.AddAsync(fitnessClass, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return fitnessClass;
    }
}
