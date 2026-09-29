using WestCoastFitness.Application.Accounts;
using WestCoastFitness.Domain;

namespace WestCoastFitness.Application.Tests;

public class AccountServiceTests
{
    private const string GoodPassword = "correct-horse-battery";

    private static AccountService Create(TestClub club) =>
        new(club.AccountRepo, club.MemberRepo, club.PlanRepo, new FakeHasher(), new FakeTokenIssuer(), club.UnitOfWork, club.Clock);

    private static RegisterRequest Request(string email = "New.Member@Example.com", string password = GoodPassword, string first = "New", string last = "Member") =>
        new(email, password, first, last, "602-555-0100");

    [Fact]
    public async Task Register_CreatesAMemberOnTheCheapestPlan()
    {
        var club = new TestClub();

        var result = await Create(club).RegisterAsync(Request(), CancellationToken.None);

        var member = Assert.Single(club.Members);
        Assert.Equal(club.Basic.Id, member.MembershipPlanId);
        Assert.Equal("new.member@example.com", member.Email);
        Assert.Equal(member.Id, result.MemberId);
        Assert.Equal(UserRole.Member, result.Role);
        Assert.Equal("token-for-new.member@example.com", result.Token);
        Assert.Equal("hashed:" + GoodPassword, Assert.Single(club.Accounts).PasswordHash);
        Assert.Equal(1, club.SaveCount);
    }

    [Fact]
    public async Task Register_RejectsADuplicateEmail()
    {
        var club = new TestClub();
        var service = Create(club);
        await service.RegisterAsync(Request(), CancellationToken.None);

        var ex = await Assert.ThrowsAsync<ClubException>(() => service.RegisterAsync(Request("NEW.member@example.com"), CancellationToken.None));

        Assert.Equal(ClubErrorKind.Conflict, ex.Kind);
    }

    [Theory]
    [InlineData("not-an-email", GoodPassword, "New", "Member")]
    [InlineData("a@example.com", "short", "New", "Member")]
    [InlineData("a@example.com", GoodPassword, " ", "Member")]
    [InlineData("a@example.com", GoodPassword, "New", "")]
    public async Task Register_ValidatesInput(string email, string password, string first, string last)
    {
        var club = new TestClub();

        var ex = await Assert.ThrowsAsync<ClubException>(() => Create(club).RegisterAsync(Request(email, password, first, last), CancellationToken.None));

        Assert.Equal(ClubErrorKind.Validation, ex.Kind);
        Assert.Empty(club.Members);
    }

    [Fact]
    public async Task Register_FailsWhenNoPlansExist()
    {
        var club = new TestClub();
        club.Plans.Clear();

        var ex = await Assert.ThrowsAsync<ClubException>(() => Create(club).RegisterAsync(Request(), CancellationToken.None));

        Assert.Equal(ClubErrorKind.Conflict, ex.Kind);
    }

    [Fact]
    public async Task Login_ReturnsATokenForValidCredentials()
    {
        var club = new TestClub();
        var service = Create(club);
        await service.RegisterAsync(Request(), CancellationToken.None);

        var result = await service.LoginAsync(new LoginRequest(" New.Member@example.com ", GoodPassword), CancellationToken.None);

        Assert.Equal("token-for-new.member@example.com", result.Token);
    }

    [Theory]
    [InlineData("new.member@example.com", "wrong-password-value")]
    [InlineData("nobody@example.com", GoodPassword)]
    public async Task Login_RejectsBadCredentialsWithTheSameError(string email, string password)
    {
        var club = new TestClub();
        var service = Create(club);
        await service.RegisterAsync(Request(), CancellationToken.None);

        var ex = await Assert.ThrowsAsync<ClubException>(() => service.LoginAsync(new LoginRequest(email, password), CancellationToken.None));

        Assert.Equal(ClubErrorKind.Unauthorized, ex.Kind);
        Assert.Equal("Email or password is incorrect.", ex.Message);
    }

    [Fact]
    public async Task NullRequests_Throw()
    {
        var service = Create(new TestClub());

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.RegisterAsync(null!, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.LoginAsync(null!, CancellationToken.None));
    }
}
