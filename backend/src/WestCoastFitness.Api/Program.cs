using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using WestCoastFitness.Api;
using WestCoastFitness.Application;
using WestCoastFitness.Application.Accounts;
using WestCoastFitness.Application.Catalog;
using WestCoastFitness.Application.Membership;
using WestCoastFitness.Application.Payments;
using WestCoastFitness.Application.Scheduling;
using WestCoastFitness.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Secrets (Jwt:SigningKey, ConnectionStrings:Postgres, Admin:*) come from user-secrets or environment
// variables. Nothing sensitive is committed to appsettings.json.
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
if (jwt.SigningKey.Length < JwtOptions.MinimumKeyLength)
{
    throw new InvalidOperationException($"Jwt:SigningKey must be at least {JwtOptions.MinimumKeyLength} characters.");
}

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<CatalogService>();
builder.Services.AddScoped<BookingService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<MembershipService>();
builder.Services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            NameClaimType = ClaimNames.Subject,
            RoleClaimType = ClaimNames.Role,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddClubRateLimiting(builder.Configuration);

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
    options.AddPolicy("frontend", policy => policy
        .WithOrigins(allowedOrigins)
        .WithHeaders("Authorization", "Content-Type")
        .WithMethods("GET", "POST", "PUT", "DELETE")));

var app = builder.Build();

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<ClubExceptionMiddleware>();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRateLimiter();
app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).DisableRateLimiting();

await AdminBootstrapper.EnsureAdministratorAsync(app.Services, app.Configuration, CancellationToken.None);
await app.RunAsync();

/// <summary>Entry-point marker so the integration tests can host the application.</summary>
public partial class Program;
