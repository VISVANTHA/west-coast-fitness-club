using System.Net;
using System.Net.Http.Json;
using WestCoastFitness.Api.Controllers;
using WestCoastFitness.Application.Catalog;
using WestCoastFitness.Application.Membership;
using WestCoastFitness.Application.Scheduling;
using WestCoastFitness.Domain;

namespace WestCoastFitness.Api.Tests;

public class MemberFlowTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static Uri U(string path) => new(path, UriKind.Relative);

    [Fact]
    public async Task Catalogue_IsPublic()
    {
        var client = factory.CreateClient();

        var plans = await client.GetFromJsonAsync<List<MembershipPlan>>(U("/api/plans"), ApiFactory.JsonOptions);
        var trainers = await client.GetFromJsonAsync<List<Trainer>>(U("/api/trainers"), ApiFactory.JsonOptions);
        var classes = await client.GetFromJsonAsync<List<ClassSummary>>(U("/api/classes"), ApiFactory.JsonOptions);

        Assert.Equal(["Basic", "Premium"], plans!.Select(p => p.Name));
        Assert.Single(trainers!);
        Assert.Equal(2, classes!.Count);
        Assert.Equal("Dana Whitfield", classes[0].TrainerName);
    }

    [Fact]
    public async Task Member_CanReadOwnProfileAndSummary()
    {
        var auth = await factory.RegisterAsync(factory.CreateClient());
        var client = factory.ClientFor(auth);

        var profile = await client.GetFromJsonAsync<MemberProfile>(U($"/api/members/{auth.MemberId}"), ApiFactory.JsonOptions);
        var summary = await client.GetFromJsonAsync<MembershipSummary>(U($"/api/members/{auth.MemberId}/summary"), ApiFactory.JsonOptions);

        Assert.Equal(auth.MemberId, profile!.Id);
        Assert.Equal("Basic", summary!.PlanName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("/summary")]
    [InlineData("/bookings")]
    [InlineData("/payments")]
    public async Task Member_CannotReadAnotherMembersRecords_Bola(string suffix)
    {
        var owner = await factory.RegisterAsync(factory.CreateClient());
        var intruder = await factory.RegisterAsync(factory.CreateClient());

        var denied = await factory.ClientFor(intruder).GetAsync(U($"/api/members/{owner.MemberId}{suffix}"));
        var allowed = await factory.ClientFor(owner).GetAsync(U($"/api/members/{owner.MemberId}{suffix}"));

        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
    }

    [Fact]
    public async Task Member_CannotBookOrChargeOnBehalfOfAnotherMember_Bola()
    {
        var owner = await factory.RegisterAsync(factory.CreateClient());
        var intruder = await factory.RegisterAsync(factory.CreateClient());
        var client = factory.ClientFor(intruder);

        var book = await client.PostAsJsonAsync($"/api/members/{owner.MemberId}/bookings", new BookClassRequest(factory.ClassId), ApiFactory.JsonOptions);
        var charge = await client.PostAsJsonAsync($"/api/members/{owner.MemberId}/payments", new Application.Payments.ChargeRequest(10m, "tok_visa", "x"), ApiFactory.JsonOptions);
        var change = await client.PutAsJsonAsync($"/api/members/{owner.MemberId}/plan", new ChangePlanRequest(factory.PremiumPlanId, "tok_visa"), ApiFactory.JsonOptions);

        Assert.Equal(HttpStatusCode.Forbidden, book.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, charge.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, change.StatusCode);
    }

    [Fact]
    public async Task Booking_LifecycleAndConflicts()
    {
        var auth = await factory.RegisterAsync(factory.CreateClient());
        var other = await factory.RegisterAsync(factory.CreateClient());
        var client = factory.ClientFor(auth);
        var path = $"/api/members/{auth.MemberId}/bookings";

        var booked = await client.PostAsJsonAsync(path, new BookClassRequest(factory.ClassId), ApiFactory.JsonOptions);
        var duplicate = await client.PostAsJsonAsync(path, new BookClassRequest(factory.ClassId), ApiFactory.JsonOptions);
        var overlap = await client.PostAsJsonAsync(path, new BookClassRequest(factory.OverlappingClassId), ApiFactory.JsonOptions);
        var missing = await client.PostAsJsonAsync(path, new BookClassRequest(Guid.NewGuid()), ApiFactory.JsonOptions);
        var view = (await booked.Content.ReadFromJsonAsync<BookingView>(ApiFactory.JsonOptions))!;

        Assert.Equal(HttpStatusCode.Created, booked.StatusCode);
        Assert.Equal(BookingStatus.Confirmed, view.Status);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, overlap.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Single((await client.GetFromJsonAsync<List<BookingView>>(U(path), ApiFactory.JsonOptions))!);

        Assert.Equal(HttpStatusCode.Forbidden, (await factory.ClientFor(other).DeleteAsync(U($"/api/bookings/{view.Id}"))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync(U($"/api/bookings/{view.Id}"))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync(U($"/api/bookings/{view.Id}"))).StatusCode);
    }

    [Fact]
    public async Task Payments_ApproveValidChargesAndDeclineZeroAmounts()
    {
        var auth = await factory.RegisterAsync(factory.CreateClient());
        var client = factory.ClientFor(auth);
        var path = $"/api/members/{auth.MemberId}/payments";

        var approved = await client.PostAsJsonAsync(path, new Application.Payments.ChargeRequest(19.99m, "tok_visa", "Guest pass"), ApiFactory.JsonOptions);
        var zero = await client.PostAsJsonAsync(path, new Application.Payments.ChargeRequest(0m, "tok_visa", "Free"), ApiFactory.JsonOptions);
        var negative = await client.PostAsJsonAsync(path, new Application.Payments.ChargeRequest(-5m, "tok_visa", "Refund?"), ApiFactory.JsonOptions);
        var history = await client.GetFromJsonAsync<List<Application.Payments.PaymentView>>(U(path), ApiFactory.JsonOptions);

        Assert.Equal(HttpStatusCode.Created, approved.StatusCode);
        Assert.Equal(HttpStatusCode.PaymentRequired, zero.StatusCode);
        Assert.Equal(HttpStatusCode.PaymentRequired, negative.StatusCode);
        Assert.Equal(3, history!.Count);
        Assert.Equal(2, history.Count(p => p.Status == PaymentStatus.Declined));
    }

    [Fact]
    public async Task ChangePlan_UpgradeChargesAndDowngradeIsFree()
    {
        var auth = await factory.RegisterAsync(factory.CreateClient());
        var client = factory.ClientFor(auth);
        var path = $"/api/members/{auth.MemberId}/plan";

        var upgrade = await client.PutAsJsonAsync(path, new ChangePlanRequest(factory.PremiumPlanId, "tok_visa"), ApiFactory.JsonOptions);
        var result = await upgrade.Content.ReadFromJsonAsync<PlanChangeResult>(ApiFactory.JsonOptions);
        var downgrade = await client.PutAsJsonAsync(path, new ChangePlanRequest(factory.BasicPlanId, "tok_visa"), ApiFactory.JsonOptions);

        Assert.Equal(HttpStatusCode.OK, upgrade.StatusCode);
        Assert.True(result!.AmountCharged > 0m);
        Assert.Equal(HttpStatusCode.OK, downgrade.StatusCode);
        Assert.Equal(0m, (await downgrade.Content.ReadFromJsonAsync<PlanChangeResult>(ApiFactory.JsonOptions))!.AmountCharged);

        var badToken = await client.PutAsJsonAsync(path, new ChangePlanRequest(factory.PremiumPlanId, "tok_decline"), ApiFactory.JsonOptions);
        Assert.Equal(HttpStatusCode.PaymentRequired, badToken.StatusCode);
    }

    [Fact]
    public async Task Administrator_CanListMembersAndReachAnyMember()
    {
        var admin = await ApiFactory.LoginAdminAsync(factory.CreateClient());
        var member = await factory.RegisterAsync(factory.CreateClient());
        var client = factory.ClientFor(admin);

        var members = await client.GetFromJsonAsync<List<MemberProfile>>(U("/api/members"), ApiFactory.JsonOptions);
        var bookings = await client.GetAsync(U($"/api/members/{member.MemberId}/bookings"));

        Assert.Contains(members!, m => m.Id == member.MemberId);
        Assert.Equal(HttpStatusCode.OK, bookings.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await factory.ClientFor(member).GetAsync(U("/api/members"))).StatusCode);
    }

    [Fact]
    public async Task ClassManagement_IsAdministratorOnly()
    {
        var admin = await ApiFactory.LoginAdminAsync(factory.CreateClient());
        var member = await factory.RegisterAsync(factory.CreateClient());
        var request = new CreateClassRequest("Sunrise Yoga", factory.TrainerId, "Scottsdale", DateTimeOffset.UtcNow.AddDays(3), 60, 15);

        var denied = await factory.ClientFor(member).PostAsJsonAsync("/api/classes", request, ApiFactory.JsonOptions);
        var created = await factory.ClientFor(admin).PostAsJsonAsync("/api/classes", request, ApiFactory.JsonOptions);
        var invalid = await factory.ClientFor(admin).PostAsJsonAsync("/api/classes", request with { Capacity = 0 }, ApiFactory.JsonOptions);
        var announcements = await factory.ClientFor(admin).GetFromJsonAsync<List<string>>(U("/api/classes/staff-announcements"), ApiFactory.JsonOptions);
        var memberAnnouncements = await factory.ClientFor(member).GetAsync(U("/api/classes/staff-announcements"));

        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Contains(announcements!, a => a.Contains("Sunrise Yoga", StringComparison.Ordinal));
        Assert.Equal(HttpStatusCode.Forbidden, memberAnnouncements.StatusCode);
    }

    [Fact]
    public async Task MalformedBodies_AreBadRequests()
    {
        var auth = await factory.RegisterAsync(factory.CreateClient());
        var client = factory.ClientFor(auth);
        using var content = new StringContent("{ not json", System.Text.Encoding.UTF8, "application/json");

        var response = await client.PostAsync(U($"/api/members/{auth.MemberId}/bookings"), content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
