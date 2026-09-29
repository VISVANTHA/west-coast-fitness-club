using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WestCoastFitness.Application;
using WestCoastFitness.Domain;

namespace WestCoastFitness.Infrastructure;

/// <summary>
/// Creates the first administrator from configuration (Admin:Email and Admin:Password, supplied through
/// user-secrets or environment variables). Does nothing when they are absent or the account exists.
/// </summary>
public static class AdminBootstrapper
{
    public static async Task<bool> EnsureAdministratorAsync(IServiceProvider services, IConfiguration configuration, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var email = configuration["Admin:Email"]?.Trim().ToLowerInvariant();
        var password = configuration["Admin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
        {
            return false;
        }

        using var scope = services.CreateScope();
        var accounts = scope.ServiceProvider.GetRequiredService<IUserAccountRepository>();
        if (await accounts.FindByEmailAsync(email, cancellationToken) is not null)
        {
            return false;
        }

        var hasher = scope.ServiceProvider.GetRequiredService<Application.IPasswordHasher>();
        await accounts.AddAsync(
            new UserAccount { Id = Guid.NewGuid(), Email = email, PasswordHash = hasher.Hash(password), Role = UserRole.Administrator },
            cancellationToken);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(cancellationToken);
        return true;
    }
}
