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

        // INTENTIONAL ISSUE (secrets hygiene): when ConnectionStrings:Postgres is not configured, a hardcoded local
        // development connection string is used instead of failing. It is a placeholder for a throwaway local
        // database, never a real credential. See docs/clarifications.md, "Seeded findings".
        var connection = configuration.GetConnectionString("Postgres");
        if (string.IsNullOrWhiteSpace(connection))
        {
            connection = "Host=localhost;Port=5432;Database=westcoastfitness;Username=fitness;Password=changeme-local-only";
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
