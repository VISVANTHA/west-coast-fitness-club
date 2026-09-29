using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WestCoastFitness.Infrastructure;

/// <summary>
/// Design-time factory for dotnet-ef (for example <c>dotnet ef dbcontext script</c>). It reads the
/// connection string from the environment; no credential is stored in source.
/// </summary>
public sealed class FitnessDbContextFactory : IDesignTimeDbContextFactory<FitnessDbContext>
{
    public FitnessDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? "Host=localhost;Database=westcoastfitness_design";
        var options = new DbContextOptionsBuilder<FitnessDbContext>().UseNpgsql(connection).Options;
        return new FitnessDbContext(options);
    }
}
