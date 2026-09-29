using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WestCoastFitness.Application.Catalog;
using WestCoastFitness.Domain;

namespace WestCoastFitness.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class CatalogController(CatalogService catalog) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("plans")]
    public async Task<IReadOnlyList<MembershipPlan>> Plans(CancellationToken cancellationToken) =>
        await catalog.ListPlansAsync(cancellationToken);

    [AllowAnonymous]
    [HttpGet("trainers")]
    public async Task<IReadOnlyList<Trainer>> Trainers(CancellationToken cancellationToken) =>
        await catalog.ListTrainersAsync(cancellationToken);

    [AllowAnonymous]
    [HttpGet("classes")]
    public async Task<IReadOnlyList<ClassSummary>> Classes(CancellationToken cancellationToken) =>
        await catalog.ListUpcomingClassesAsync(cancellationToken);

    [Authorize]
    [HttpGet("classes/staff-announcements")]
    public async Task<IReadOnlyList<string>> StaffAnnouncements(CancellationToken cancellationToken) =>
        await catalog.ListStaffAnnouncementsAsync(User.ToCaller(), cancellationToken);

    [Authorize]
    [HttpPost("classes")]
    public async Task<ActionResult<FitnessClass>> CreateClass(CreateClassRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await catalog.CreateClassAsync(User.ToCaller(), request, cancellationToken));
}
