using Microsoft.Extensions.DependencyInjection;
using WestCoastFitness.Application;
using WestCoastFitness.Domain;
using WestCoastFitness.Infrastructure;

namespace WestCoastFitness.Infrastructure.Tests;

public class AdminBootstrapperTests
{
    [Fact]
    public async Task DoesNothingWithoutConfiguration()
    {
        using var fixture = new InfrastructureFixture();

        var created = await AdminBootstrapper.EnsureAdministratorAsync(fixture.Services, fixture.Configuration, CancellationToken.None);

        Assert.False(created);
    }

    [Fact]
    public async Task CreatesTheAdministratorOnceAndHashesThePassword()
    {
        using var fixture = new InfrastructureFixture(new Dictionary<string, string?>
        {
            ["Admin:Email"] = " Admin@Example.com ",
            ["Admin:Password"] = "a-long-admin-passphrase",
        });

        var first = await AdminBootstrapper.EnsureAdministratorAsync(fixture.Services, fixture.Configuration, CancellationToken.None);
        var second = await AdminBootstrapper.EnsureAdministratorAsync(fixture.Services, fixture.Configuration, CancellationToken.None);

        Assert.True(first);
        Assert.False(second);
        using var scope = fixture.NewScope();
        var account = await scope.ServiceProvider.GetRequiredService<IUserAccountRepository>().FindByEmailAsync("admin@example.com", CancellationToken.None);
        Assert.Equal(UserRole.Administrator, account!.Role);
        Assert.Null(account.MemberId);
        Assert.NotEqual("a-long-admin-passphrase", account.PasswordHash);
        Assert.True(scope.ServiceProvider.GetRequiredService<Application.IPasswordHasher>().Verify(account.PasswordHash, "a-long-admin-passphrase"));
    }

    [Fact]
    public async Task NullArgumentsThrow()
    {
        using var fixture = new InfrastructureFixture();

        await Assert.ThrowsAsync<ArgumentNullException>(() => AdminBootstrapper.EnsureAdministratorAsync(null!, fixture.Configuration, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentNullException>(() => AdminBootstrapper.EnsureAdministratorAsync(fixture.Services, null!, CancellationToken.None));
    }
}
