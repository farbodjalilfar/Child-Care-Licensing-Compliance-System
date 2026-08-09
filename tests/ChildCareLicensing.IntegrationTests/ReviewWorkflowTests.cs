using System.Net;
using System.Net.Http.Json;
using ChildCareLicensing.Application.Abstractions;
using ChildCareLicensing.Application.LicenceApplications;
using ChildCareLicensing.Application.PublicRegistry;

namespace ChildCareLicensing.IntegrationTests;

/// <summary>
/// Walks an application through its whole life: the operator submits, a reviewer sends it
/// back, the operator resubmits, and the reviewer approves and issues the licence. This
/// class owns its host because every step mutates state.
/// </summary>
public class ReviewWorkflowTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string ApplicationRoot = "/api/licence-applications";
    private const string ReviewRoot = "/api/review/licence-applications";

    private readonly HttpClient operatorClient = factory.CreateClientAs(
        TestIdentities.OperatorRole,
        TestIdentities.SunshineOperatorId,
        "maria@sunshinechildcare.example");

    private readonly HttpClient reviewerClient = factory.CreateClientAs(
        TestIdentities.ReviewerRole,
        email: "j.tremblay@ontario.example");

    [Fact]
    public async Task ApplicationTravelsFromSubmissionToIssuedLicence()
    {
        var applicationId = TestIdentities.SunshineApplicationId;

        // The operator submits.
        var submit = await operatorClient.PostAsync($"{ApplicationRoot}/{applicationId}/submit", null);
        submit.EnsureSuccessStatusCode();

        // It shows up in the reviewer's queue.
        var queue = await reviewerClient.GetFromJsonAsync<List<ReviewQueueItem>>($"{ReviewRoot}/queue");
        Assert.NotNull(queue);
        Assert.Contains(queue, q => q.ApplicationId == applicationId && q.Status == "Submitted");

        // Approving before the review has started is refused: the state machine says so.
        var earlyApproval = await Decide(reviewerClient, applicationId, "approve", null);
        Assert.Equal(HttpStatusCode.BadRequest, earlyApproval.StatusCode);

        await AssertSucceeds(await Decide(reviewerClient, applicationId, "start-review", null));

        // Sent back for more information, with a reason the operator can read.
        await AssertSucceeds(await Decide(
            reviewerClient, applicationId, "request-information", "Confirm the fire inspection date."));

        var afterRequest = await GetApplication(operatorClient, applicationId);
        Assert.Equal("AdditionalInfoRequired", afterRequest.Status);
        Assert.Equal("Confirm the fire inspection date.", afterRequest.ReviewerNotes);

        // The operator can resubmit from that state.
        var resubmit = await operatorClient.PostAsync($"{ApplicationRoot}/{applicationId}/submit", null);
        resubmit.EnsureSuccessStatusCode();

        await AssertSucceeds(await Decide(reviewerClient, applicationId, "start-review", null));

        var approval = await Decide(reviewerClient, applicationId, "approve", "Capacity verified.");
        var approvalResult = await AssertSucceeds(approval);

        Assert.Equal("Approved", approvalResult.Status);
        Assert.NotNull(approvalResult.LicenceNumber);
        Assert.StartsWith("ON-CCL-", approvalResult.LicenceNumber);

        // The decision is recorded end to end.
        var history = await reviewerClient.GetFromJsonAsync<List<ApplicationHistoryEntry>>(
            $"{ReviewRoot}/{applicationId}/history");

        Assert.NotNull(history);
        Assert.Contains(history, h => h.ToStatus == "Approved" && h.ChangedBy == "j.tremblay@ontario.example");
        Assert.Contains(history, h => h.ToStatus == "AdditionalInfoRequired");

        // And the newly licensed centre reaches the public register.
        var listings = await factory.CreateClient()
            .GetFromJsonAsync<List<PublicFacilityListing>>("/api/public/facilities?name=Sunshine");

        Assert.NotNull(listings);
        Assert.Contains(listings, l => l.LicenceNumber == approvalResult.LicenceNumber);
    }

    [Fact]
    public async Task RequestingInformationWithoutAReasonIsRefused()
    {
        var response = await Decide(
            reviewerClient, TestIdentities.SunshineApplicationId, "request-information", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DecidingAnUnknownApplicationReturns404()
    {
        var response = await Decide(reviewerClient, Guid.NewGuid(), "start-review", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static Task<HttpResponseMessage> Decide(
        HttpClient client,
        Guid applicationId,
        string action,
        string? notes)
        => client.PostAsJsonAsync($"{ReviewRoot}/{applicationId}/{action}", new { Notes = notes });

    private static async Task<ReviewDecisionResult> AssertSucceeds(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ReviewDecisionResult>();
        Assert.NotNull(result);
        Assert.True(result.Succeeded, result.ErrorMessage);

        return result;
    }

    private static async Task<LicenceApplicationDetails> GetApplication(HttpClient client, Guid applicationId)
    {
        var application = await client.GetFromJsonAsync<LicenceApplicationDetails>(
            $"{ApplicationRoot}/{applicationId}");

        Assert.NotNull(application);
        return application;
    }
}
