using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WestCoastFitness.Application;

namespace WestCoastFitness.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registers EF Core, the repositories and the in-process adapters.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // The connection string comes from user-secrets or an environment variable, never from source.
        var connection = configuration.GetConnectionString("Postgres");
        if (string.IsNullOrWhiteSpace(connection))
        {
            throw new InvalidOperationException("ConnectionStrings:Postgres is not configured.");
        }

        services.AddDbContext<FitnessDbContext>(options => options.UseNpgsql(connection));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<Application.IPasswordHasher, IdentityPasswordHasher>();
        services.AddSingleton<IPaymentGateway, MockPaymentGateway>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IMemberRepository, MemberRepository>();
        services.AddScoped<IMembershipPlanRepository, MembershipPlanRepository>();
        services.AddScoped<ITrainerRepository, TrainerRepository>();
        services.AddScoped<IFitnessClassRepository, FitnessClassRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IUserAccountRepository, UserAccountRepository>();
        return services;
    }
}
