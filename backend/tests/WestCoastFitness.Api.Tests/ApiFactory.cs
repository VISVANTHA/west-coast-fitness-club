using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WestCoastFitness.Application.Accounts;
using WestCoastFitness.Domain;
using WestCoastFitness.Infrastructure;

namespace WestCoastFitness.Api.Tests;

/// <summary>Hosts the real API pipeline on an isolated in-memory database with seeded catalogue data.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string SigningKey = "test-signing-key-that-is-at-least-32-chars";
    public const string AdminEmail = "admin@example.com";
    public const string AdminPassword = "an-administrator-passphrase";
    public const string MemberPassword = "correct-horse-battery";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _databaseName = Guid.NewGuid().ToString("N");
    private readonly int _authPermitLimit;
    private int _counter;

    public ApiFactory()
        : this(1000)
    {
    }

    internal ApiFactory(int authPermitLimit)
    {
        _authPermitLimit = authPermitLimit;
    }

    public static JsonSerializerOptions JsonOptions => Json;

    public Guid BasicPlanId { get; private set; }

    public Guid PremiumPlanId { get; private set; }

    public Guid TrainerId { get; private set; }

    public Guid ClassId { get; private set; }

    public Guid OverlappingClassId { get; private set; }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FitnessDbContext>();
        var basic = new MembershipPlan { Id = Guid.NewGuid(), Name = "Basic", MonthlyPrice = 29.99m, MaxClassesPerWeek = 3 };
        var premium = new MembershipPlan { Id = Guid.NewGuid(), Name = "Premium", MonthlyPrice = 79.99m, MaxClassesPerWeek = null };
        var trainer = new Trainer { Id = Guid.NewGuid(), FullName = "Dana Whitfield", Specialty = "Cycling" };
        var start = DateTimeOffset.UtcNow.AddDays(30).Date.AddHours(17);
        var fitnessClass = new FitnessClass { Id = Guid.NewGuid(), Title = "Desert Spin", TrainerId = trainer.Id, Location = "Phoenix", StartsAt = new DateTimeOffset(start, TimeSpan.Zero), DurationMinutes = 45, Capacity = 20 };
        var overlapping = new FitnessClass { Id = Guid.NewGuid(), Title = "Saguaro HIIT", TrainerId = trainer.Id, Location = "Tucson", StartsAt = fitnessClass.StartsAt.AddMinutes(15), DurationMinutes = 45, Capacity = 20 };
        db.AddRange(basic, premium, trainer, fitnessClass, overlapping);
        await db.SaveChangesAsync();
        (BasicPlanId, PremiumPlanId, TrainerId, ClassId, OverlappingClassId) = (basic.Id, premium.Id, trainer.Id, fitnessClass.Id, overlapping.Id);
    }

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;

    public string NextEmail() => $"member{Interlocked.Increment(ref _counter)}@example.com";

    public async Task<AuthResult> RegisterAsync(HttpClient client, string? email = null)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email ?? NextEmail(), MemberPassword, "Test", "Member", "602-555-0100"),
            Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResult>(Json))!;
    }

    public static async Task<AuthResult> LoginAdminAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(AdminEmail, AdminPassword), Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResult>(Json))!;
    }

    public HttpClient ClientFor(AuthResult? auth = null)
    {
        var client = CreateClient();
        if (auth is not null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        }

        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.UseSetting("Jwt:SigningKey", SigningKey);
        builder.UseSetting("ConnectionStrings:Postgres", "Host=unused;Database=unused");
        builder.UseSetting("Admin:Email", AdminEmail);
        builder.UseSetting("Admin:Password", AdminPassword);
        builder.UseSetting("RateLimiting:AuthPermitLimit", _authPermitLimit.ToString(System.Globalization.CultureInfo.InvariantCulture));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<FitnessDbContext>>();
            services.AddDbContext<FitnessDbContext>(o => o.UseInMemoryDatabase(_databaseName));
        });
    }
}
