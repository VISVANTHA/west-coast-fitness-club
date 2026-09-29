using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using WestCoastFitness.Application.Accounts;

namespace WestCoastFitness.Api.Tests;

public class AuthAndSecurityTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static string Token(IEnumerable<KeyValuePair<string, object>> claims, string key = ApiFactory.SigningKey)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = "west-coast-fitness",
            Audience = "west-coast-fitness-members",
            Expires = DateTime.UtcNow.AddMinutes(5),
            Claims = claims.ToDictionary(c => c.Key, c => c.Value),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256),
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await factory.CreateClient().GetAsync(new Uri("/health", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Responses_CarrySecurityHeaders()
    {
        var response = await factory.CreateClient().GetAsync(new Uri("/health", UriKind.Relative));

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
        Assert.Contains("default-src 'none'", response.Headers.GetValues("Content-Security-Policy").Single());
        Assert.False(response.Headers.Contains("Strict-Transport-Security"));
    }

    [Fact]
    public async Task Hsts_IsSentOverHttps()
    {
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

        var response = await client.GetAsync(new Uri("/health", UriKind.Relative));

        Assert.Contains("max-age=31536000", response.Headers.GetValues("Strict-Transport-Security").Single());
    }

    [Fact]
    public async Task Swagger_IsServedInDevelopmentWithoutTheApiCsp()
    {
        var response = await factory.CreateClient().GetAsync(new Uri("/swagger/v1/swagger.json", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("Content-Security-Policy"));
    }

    [Fact]
    public async Task Cors_AllowsOnlyTheConfiguredOrigin()
    {
        async Task<HttpResponseMessage> Preflight(string origin)
        {
            var request = new HttpRequestMessage(HttpMethod.Options, "/api/plans");
            request.Headers.Add("Origin", origin);
            request.Headers.Add("Access-Control-Request-Method", "GET");
            return await factory.CreateClient().SendAsync(request);
        }

        var allowed = await Preflight("http://localhost:5173");
        var blocked = await Preflight("https://evil.example");

        Assert.Equal("http://localhost:5173", allowed.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.False(blocked.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Theory]
    [InlineData("/api/members")]
    [InlineData("/api/members/00000000-0000-0000-0000-000000000001")]
    [InlineData("/api/members/00000000-0000-0000-0000-000000000001/bookings")]
    [InlineData("/api/members/00000000-0000-0000-0000-000000000001/payments")]
    [InlineData("/api/classes/staff-announcements")]
    public async Task ProtectedEndpoints_RequireAToken(string path)
    {
        var response = await factory.CreateClient().GetAsync(new Uri(path, UriKind.Relative));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ForgedOrMalformedTokens_AreRejected()
    {
        var forged = Token(
            [new("sub", Guid.NewGuid().ToString()), new("role", "Administrator")],
            key: "a-different-signing-key-of-32-plus-characters");
        var client = factory.CreateClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", forged);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(new Uri("/api/members", UriKind.Relative))).StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not.a.jwt");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(new Uri("/api/members", UriKind.Relative))).StatusCode);
    }

    [Fact]
    public async Task ATokenWithoutRequiredClaims_IsRejected()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token([new("sub", Guid.NewGuid().ToString())]));

        var response = await client.GetAsync(new Uri("/api/members", UriKind.Relative));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_ThenLogin_ReturnsTokens()
    {
        var client = factory.CreateClient();
        var email = factory.NextEmail();

        var registered = await factory.RegisterAsync(client, email);
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, ApiFactory.MemberPassword), ApiFactory.JsonOptions);
        var login = await response.Content.ReadFromJsonAsync<AuthResult>(ApiFactory.JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(registered.MemberId, login!.MemberId);
        Assert.Equal(Domain.UserRole.Member, login.Role);
    }

    [Fact]
    public async Task Register_DuplicateEmailAndBadInput_AreRejected()
    {
        var client = factory.CreateClient();
        var email = factory.NextEmail();
        await factory.RegisterAsync(client, email);

        var duplicate = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, ApiFactory.MemberPassword, "A", "B", "1"), ApiFactory.JsonOptions);
        var weak = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(factory.NextEmail(), "short", "A", "B", "1"), ApiFactory.JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, weak.StatusCode);
    }

    [Fact]
    public async Task Login_WithBadCredentials_IsUnauthorizedWithAProblemBody()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest("nobody@example.com", "whatever-password"), ApiFactory.JsonOptions);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("incorrect", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task AdministratorFromConfiguration_CanLogIn()
    {
        var admin = await ApiFactory.LoginAdminAsync(factory.CreateClient());

        Assert.Equal(Domain.UserRole.Administrator, admin.Role);
        Assert.Null(admin.MemberId);
    }

    [Fact]
    public async Task AuthEndpoints_AreRateLimited()
    {
        await using var limited = new ApiFactory(authPermitLimit: 3);
        await ((IAsyncLifetime)limited).InitializeAsync();
        var client = limited.CreateClient();
        var attempt = new LoginRequest("nobody@example.com", "whatever-password");

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 5; i++)
        {
            statuses.Add((await client.PostAsJsonAsync("/api/auth/login", attempt, ApiFactory.JsonOptions)).StatusCode);
        }

        Assert.Equal(3, statuses.Count(s => s == HttpStatusCode.Unauthorized));
        Assert.Equal(2, statuses.Count(s => s == HttpStatusCode.TooManyRequests));
    }

    [Fact]
    public void Startup_RejectsAWeakSigningKey()
    {
        using var weak = new ApiFactory().WithWebHostBuilder(b => b.UseSetting("Jwt:SigningKey", "too-short"));

        Assert.Throws<InvalidOperationException>(() => weak.CreateClient());
    }
}
