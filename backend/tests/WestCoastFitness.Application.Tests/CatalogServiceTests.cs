using WestCoastFitness.Application.Catalog;
using WestCoastFitness.Domain;

namespace WestCoastFitness.Application.Tests;

public class CatalogServiceTests
{
    private static CatalogService Create(TestClub club) =>
        new(club.PlanRepo, club.TrainerRepo, club.ClassRepo, club.BookingRepo, club.UnitOfWork, club.Clock);

    private static CreateClassRequest ValidRequest(TestClub club) =>
        new("Sunrise Yoga", club.Trainer.Id, "Scottsdale", TestClub.Now.AddDays(2), 60, 20);

    [Fact]
    public async Task Plans_AreOrderedByPrice()
    {
        var plans = await Create(new TestClub()).ListPlansAsync(CancellationToken.None);

        Assert.Equal(["Basic", "Premium"], plans.Select(p => p.Name));
    }

    [Fact]
    public async Task Trainers_AreListed()
    {
        Assert.Single(await Create(new TestClub()).ListTrainersAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Classes_ShowUpcomingOnlyWithRemainingSpots()
    {
        var club = new TestClub();
        var member = club.AddMember();
        club.AddClass(TestClub.Now.AddDays(-1));
        var later = club.AddClass(TestClub.Now.AddDays(3), capacity: 5);
        var sooner = club.AddClass(TestClub.Now.AddDays(1), capacity: 2);
        club.AddBooking(member, sooner);
        club.AddBooking(club.AddMember("Ben"), sooner);
        club.AddBooking(club.AddMember("Cy"), later, BookingStatus.Cancelled);

        var classes = await Create(club).ListUpcomingClassesAsync(CancellationToken.None);

        Assert.Equal([sooner.Id, later.Id], classes.Select(c => c.Id));
        Assert.Equal(0, classes[0].SpotsLeft);
        Assert.Equal(5, classes[1].SpotsLeft);
        Assert.Equal("Dana Whitfield", classes[0].TrainerName);
        Assert.Contains("Dana Whitfield", classes[0].Announcement);
    }

    [Fact]
    public async Task Classes_WithAnUnknownTrainerShowTba()
    {
        var club = new TestClub();
        club.AddClass(TestClub.Now.AddDays(1)).TrainerId = Guid.NewGuid();

        var classes = await Create(club).ListUpcomingClassesAsync(CancellationToken.None);

        Assert.Equal("TBA", Assert.Single(classes).TrainerName);
    }

    [Fact]
    public async Task StaffAnnouncements_AreAdministratorOnly()
    {
        var club = new TestClub();
        club.AddClass(TestClub.Now.AddDays(1), capacity: 12);
        var service = Create(club);

        var lines = await service.ListStaffAnnouncementsAsync(TestClub.AdminCaller(), CancellationToken.None);
        var denied = await Assert.ThrowsAsync<ClubException>(() => service.ListStaffAnnouncementsAsync(TestClub.MemberCaller(club.AddMember()), CancellationToken.None));

        Assert.Contains("12 of 12 spots open", Assert.Single(lines));
        Assert.Equal(ClubErrorKind.Forbidden, denied.Kind);
    }

    [Fact]
    public async Task CreateClass_StoresAValidClass()
    {
        var club = new TestClub();

        var created = await Create(club).CreateClassAsync(TestClub.AdminCaller(), ValidRequest(club), CancellationToken.None);

        Assert.Equal("Sunrise Yoga", created.Title);
        Assert.Contains(created, club.Classes);
        Assert.Equal(1, club.SaveCount);
    }

    [Fact]
    public async Task CreateClass_RequiresAnAdministrator()
    {
        var club = new TestClub();

        var ex = await Assert.ThrowsAsync<ClubException>(() =>
            Create(club).CreateClassAsync(TestClub.MemberCaller(club.AddMember()), ValidRequest(club), CancellationToken.None));

        Assert.Equal(ClubErrorKind.Forbidden, ex.Kind);
    }

    [Theory]
    [InlineData("", "Scottsdale", 60, 20, 2)]
    [InlineData("Yoga", " ", 60, 20, 2)]
    [InlineData("Yoga", "Scottsdale", 5, 20, 2)]
    [InlineData("Yoga", "Scottsdale", 60, 0, 2)]
    [InlineData("Yoga", "Scottsdale", 60, 20, -1)]
    public async Task CreateClass_ValidatesInput(string title, string location, int minutes, int capacity, int startsInDays)
    {
        var club = new TestClub();
        var request = new CreateClassRequest(title, club.Trainer.Id, location, TestClub.Now.AddDays(startsInDays), minutes, capacity);

        var ex = await Assert.ThrowsAsync<ClubException>(() => Create(club).CreateClassAsync(TestClub.AdminCaller(), request, CancellationToken.None));

        Assert.Equal(ClubErrorKind.Validation, ex.Kind);
    }

    [Fact]
    public async Task CreateClass_UnknownTrainer_IsNotFound()
    {
        var club = new TestClub();

        var ex = await Assert.ThrowsAsync<ClubException>(() =>
            Create(club).CreateClassAsync(TestClub.AdminCaller(), ValidRequest(club) with { TrainerId = Guid.NewGuid() }, CancellationToken.None));

        Assert.Equal(ClubErrorKind.NotFound, ex.Kind);
    }

    [Fact]
    public void Announcements_DescribeTheClassWindow()
    {
        var fitnessClass = new FitnessClass
        {
            Title = "HIIT",
            Location = "Tucson",
            StartsAt = new DateTimeOffset(2030, 1, 8, 23, 30, 0, TimeSpan.Zero),
            DurationMinutes = 90,
            Capacity = 15,
        };

        var member = ClassAnnouncementText.Describe(fitnessClass, "Ray");
        var staff = StaffClassAnnouncementText.Describe(fitnessClass, "Ray", 3);

        Assert.Contains("Join Ray for HIIT at our Tucson club", member);
        Assert.Contains("Tue Jan 8, 23:30 to Wed Jan 9, 01:00 UTC (1h 30m)", member);
        Assert.Contains("[Tucson] HIIT led by Ray, 3 of 15 spots open", staff);
    }

    [Theory]
    [InlineData(60, "(1h)")]
    [InlineData(45, "(45m)")]
    public void Announcements_FormatWholeHoursAndMinutesOnly(int minutes, string expected)
    {
        var fitnessClass = new FitnessClass { Title = "Yoga", Location = "Mesa", StartsAt = new DateTimeOffset(2030, 1, 8, 10, 0, 0, TimeSpan.Zero), DurationMinutes = minutes, Capacity = 10 };

        Assert.Contains(expected, ClassAnnouncementText.Describe(fitnessClass, "Ray"));
        Assert.Contains(expected, StaffClassAnnouncementText.Describe(fitnessClass, "Ray", 1));
    }

    [Fact]
    public void Announcements_NullClassThrows()
    {
        Assert.Throws<ArgumentNullException>(() => ClassAnnouncementText.Describe(null!, "Ray"));
        Assert.Throws<ArgumentNullException>(() => StaffClassAnnouncementText.Describe(null!, "Ray", 1));
    }

    [Fact]
    public void ClubException_FactoriesSetTheKind()
    {
        Assert.Equal(ClubErrorKind.Validation, ClubException.Validation("x").Kind);
        Assert.Equal(ClubErrorKind.Unauthorized, ClubException.Unauthorized("x").Kind);
        Assert.Equal(ClubErrorKind.Forbidden, ClubException.Forbidden("x").Kind);
        Assert.Equal(ClubErrorKind.NotFound, ClubException.NotFound("x").Kind);
        Assert.Equal(ClubErrorKind.Conflict, ClubException.Conflict("x").Kind);
        Assert.Equal(ClubErrorKind.PaymentDeclined, ClubException.PaymentDeclined("x").Kind);
    }
}
