using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WestCoastFitness.Infrastructure;

namespace WestCoastFitness.Infrastructure.Tests;

/// <summary>Builds the real infrastructure registrations on top of an isolated in-memory database.</summary>
internal sealed class InfrastructureFixture : IDisposable
{
    private readonly ServiceProvider _provider;

    public InfrastructureFixture(IReadOnlyDictionary<string, string?>? extraSettings = null)
    {
        var settings = new Dictionary<string, string?> { ["ConnectionStrings:Postgres"] = "Host=unused;Database=unused" };
        if (extraSettings is not null)
        {
            foreach (var pair in extraSettings)
            {
                settings[pair.Key] = pair.Value;
            }
        }

        Configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        services.AddInfrastructure(Configuration);
        services.RemoveAll<DbContextOptions<FitnessDbContext>>();
        var databaseName = Guid.NewGuid().ToString("N");
        services.AddDbContext<FitnessDbContext>(o => o.UseInMemoryDatabase(databaseName));
        _provider = services.BuildServiceProvider();
    }

    public IConfiguration Configuration { get; }

    public IServiceProvider Services => _provider;

    public IServiceScope NewScope() => _provider.CreateScope();

    public void Dispose() => _provider.Dispose();
}
