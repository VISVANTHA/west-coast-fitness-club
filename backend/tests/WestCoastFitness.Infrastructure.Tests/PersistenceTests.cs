using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WestCoastFitness.Application;
using WestCoastFitness.Domain;
using WestCoastFitness.Infrastructure;

namespace WestCoastFitness.Infrastructure.Tests;

public class PersistenceTests
{
    private static readonly DateTimeOffset Now = new(2030, 1, 7, 12, 0, 0, TimeSpan.Zero);

    private static async Task<(MembershipPlan Plan, Trainer Trainer, Member Member, FitnessClass Class)> SeedAsync(InfrastructureFixture fixture)
    {
        using var scope = fixture.NewScope();
        var db = scope.ServiceProvider.GetRequiredService<FitnessDbContext>();
        var plan = new MembershipPlan { Id = Guid.NewGuid(), Name = "Basic", MonthlyPrice = 29.99m, MaxClassesPerWeek = 3 };
        var trainer = new Trainer { Id = Guid.NewGuid(), FullName = "Dana Whitfield", Specialty = "Cycling" };
        var member = new Member { Id = Guid.NewGuid(), FirstName = "Ana", LastName = "Reyes", Email = "ana@example.com", MembershipPlanId = plan.Id, JoinedOn = Now };
        var fitnessClass = new FitnessClass { Id = Guid.NewGuid(), Title = "Spin", TrainerId = trainer.Id, Location = "Phoenix", StartsAt = Now.AddDays(1), DurationMinutes = 45, Capacity = 12 };
        db.AddRange(plan, trainer, member, fitnessClass);
        await db.SaveChangesAsync();
        return (plan, trainer, member, fitnessClass);
    }

    [Fact]
    public async Task Members_CanBeAddedFoundAndListedInNameOrder()
    {
        using var fixture = new InfrastructureFixture();
        var (plan, _, member, _) = await SeedAsync(fixture);
        using var scope = fixture.NewScope();
        var members = scope.ServiceProvider.GetRequiredService<IMemberRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        await members.AddAsync(new Member { Id = Guid.NewGuid(), FirstName = "Al", LastName = "Adams", Email = "al@example.com", MembershipPlanId = plan.Id }, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        Assert.Equal("Ana", (await members.FindAsync(member.Id, CancellationToken.None))!.FirstName);
        Assert.Null(await members.FindAsync(Guid.NewGuid(), CancellationToken.None));
        Assert.Equal(["Adams", "Reyes"], (await members.ListAsync(CancellationToken.None)).Select(m => m.LastName));
    }

    [Fact]
    public async Task Catalog_PlansTrainersAndUpcomingClasses()
    {
        using var fixture = new InfrastructureFixture();
        var (plan, trainer, _, fitnessClass) = await SeedAsync(fixture);
        using var scope = fixture.NewScope();
        var plans = scope.ServiceProvider.GetRequiredService<IMembershipPlanRepository>();
        var trainers = scope.ServiceProvider.GetRequiredService<ITrainerRepository>();
        var classes = scope.ServiceProvider.GetRequiredService<IFitnessClassRepository>();

        Assert.Equal(plan.Name, (await plans.FindAsync(plan.Id, CancellationToken.None))!.Name);
        Assert.Single(await plans.ListAsync(CancellationToken.None));
        Assert.Single(await trainers.ListAsync(CancellationToken.None));
        Assert.True(await trainers.ExistsAsync(trainer.Id, CancellationToken.None));
        Assert.False(await trainers.ExistsAsync(Guid.NewGuid(), CancellationToken.None));
        Assert.Equal(fitnessClass.Id, (await classes.FindAsync(fitnessClass.Id, CancellationToken.None))!.Id);
        Assert.Single(await classes.ListUpcomingAsync(Now, CancellationToken.None));
        Assert.Empty(await classes.ListUpcomingAsync(Now.AddDays(5), CancellationToken.None));

        await classes.AddAsync(new FitnessClass { Id = Guid.NewGuid(), Title = "Yoga", TrainerId = trainer.Id, Location = "Tucson", StartsAt = Now.AddDays(2), DurationMinutes = 60, Capacity = 8 }, CancellationToken.None);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);
        Assert.Equal(2, (await classes.ListUpcomingAsync(Now, CancellationToken.None)).Count);
    }

    [Fact]
    public async Task Bookings_AreCountedPerClassAndOnlyWhenConfirmed()
    {
        using var fixture = new InfrastructureFixture();
        var (_, _, member, fitnessClass) = await SeedAsync(fixture);
        using var scope = fixture.NewScope();
        var bookings = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var confirmed = new ClassBooking { Id = Guid.NewGuid(), MemberId = member.Id, FitnessClassId = fitnessClass.Id, Status = BookingStatus.Confirmed, BookedAt = Now };
        var cancelled = new ClassBooking { Id = Guid.NewGuid(), MemberId = member.Id, FitnessClassId = fitnessClass.Id, Status = BookingStatus.Cancelled, BookedAt = Now };

        await bookings.AddAsync(confirmed, CancellationToken.None);
        await bookings.AddAsync(cancelled, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        Assert.Equal(1, await bookings.CountActiveForClassAsync(fitnessClass.Id, CancellationToken.None));
        var counts = await bookings.CountActiveByClassAsync([fitnessClass.Id, Guid.NewGuid()], CancellationToken.None);
        Assert.Equal(1, counts[fitnessClass.Id]);
        Assert.Single(counts);
        var found = await bookings.FindAsync(confirmed.Id, CancellationToken.None);
        Assert.Equal("Spin", found!.FitnessClass!.Title);
        Assert.Equal(2, (await bookings.ListForMemberAsync(member.Id, CancellationToken.None)).Count);
        Assert.Empty(await bookings.ListForMemberAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task Payments_AreListedNewestFirstForTheMemberOnly()
    {
        using var fixture = new InfrastructureFixture();
        var (_, _, member, _) = await SeedAsync(fixture);
        using var scope = fixture.NewScope();
        var payments = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();

        await payments.AddAsync(new Payment { Id = Guid.NewGuid(), MemberId = member.Id, Amount = 10m, Reference = "old", CreatedAt = Now.AddDays(-2) }, CancellationToken.None);
        await payments.AddAsync(new Payment { Id = Guid.NewGuid(), MemberId = member.Id, Amount = 20m, Reference = "new", CreatedAt = Now }, CancellationToken.None);
        await payments.AddAsync(new Payment { Id = Guid.NewGuid(), MemberId = Guid.NewGuid(), Amount = 30m, Reference = "other", CreatedAt = Now }, CancellationToken.None);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);

        Assert.Equal(["new", "old"], (await payments.ListForMemberAsync(member.Id, CancellationToken.None)).Select(p => p.Reference));
    }

    [Fact]
    public async Task UserAccounts_AreFoundByEmail()
    {
        using var fixture = new InfrastructureFixture();
        using var scope = fixture.NewScope();
        var accounts = scope.ServiceProvider.GetRequiredService<IUserAccountRepository>();

        await accounts.AddAsync(new UserAccount { Id = Guid.NewGuid(), Email = "ana@example.com", PasswordHash = "h", Role = UserRole.Member }, CancellationToken.None);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);

        Assert.NotNull(await accounts.FindByEmailAsync("ana@example.com", CancellationToken.None));
        Assert.Null(await accounts.FindByEmailAsync("nobody@example.com", CancellationToken.None));
    }

    [Fact]
    public async Task Enums_AreStoredAsText()
    {
        using var fixture = new InfrastructureFixture();
        using var scope = fixture.NewScope();
        var db = scope.ServiceProvider.GetRequiredService<FitnessDbContext>();

        var status = db.Model.FindEntityType(typeof(ClassBooking))!.FindProperty(nameof(ClassBooking.Status))!;
        var role = db.Model.FindEntityType(typeof(UserAccount))!.FindProperty(nameof(UserAccount.Role))!;

        Assert.Equal(typeof(string), status.GetProviderClrType());
        Assert.Equal(typeof(string), role.GetProviderClrType());
        await Task.CompletedTask;
    }

    [Theory]
    [InlineData(typeof(Member), "members")]
    [InlineData(typeof(MembershipPlan), "membership_plans")]
    [InlineData(typeof(Trainer), "trainers")]
    [InlineData(typeof(FitnessClass), "classes")]
    [InlineData(typeof(ClassBooking), "class_bookings")]
    [InlineData(typeof(Payment), "payments")]
    [InlineData(typeof(UserAccount), "user_accounts")]
    public void Model_UsesExplicitTableNames(Type entity, string table)
    {
        using var fixture = new InfrastructureFixture();
        using var scope = fixture.NewScope();
        var db = scope.ServiceProvider.GetRequiredService<FitnessDbContext>();

        Assert.Equal(table, db.Model.FindEntityType(entity)!.GetTableName());
    }

    [Theory]
    [InlineData("FirstName", "first_name")]
    [InlineData("Id", "id")]
    [InlineData("MembershipPlanId", "membership_plan_id")]
    public void ToSnakeCase_ConvertsPascalCase(string input, string expected)
    {
        Assert.Equal(expected, FitnessDbContext.ToSnakeCase(input));
    }

    [Fact]
    public void AddInfrastructure_FallsBackToTheHardcodedLocalConnection_KnownFinding()
    {
        var services = new ServiceCollection().AddInfrastructure(new ConfigurationBuilder().Build());

        // Seeded finding (docs/clarifications.md): a missing connection string no longer fails start-up.
        Assert.Contains(services, d => d.ServiceType == typeof(FitnessDbContext));
        Assert.Throws<ArgumentNullException>(() => DependencyInjection.AddInfrastructure(null!, new ConfigurationBuilder().Build()));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddInfrastructure(null!));
    }

    [Fact]
    public void DesignTimeFactory_BuildsAnNpgsqlContext()
    {
        using var db = new FitnessDbContextFactory().CreateDbContext([]);

        Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", db.Database.ProviderName);
    }
}
