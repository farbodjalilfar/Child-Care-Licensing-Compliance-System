using System.Net;
using System.Net.Http.Json;
using ChildCareLicensing.Application.Abstractions;
using ChildCareLicensing.Application.LicenceApplications;

namespace ChildCareLicensing.IntegrationTests;

/// <summary>
/// Uses its own host so the state transition does not leak into the read-only tests.
/// </summary>
public class LicenceApplicationSubmissionTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly Guid SampleApplicationId = TestIdentities.SunshineApplicationId;

    private readonly HttpClient client = factory.CreateClientAs(
        TestIdentities.OperatorRole,
        TestIdentities.SunshineOperatorId,
        "maria@sunshinechildcare.example");

    [Fact]
    public async Task Submit_ApprovesCapacityAndBlocksResubmission()
    {
        var response = await client.PostAsync(
            $"/api/licence-applications/{SampleApplicationId}/submit",
            content: null);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<SubmitLicenceApplicationResult>();
        Assert.NotNull(result);
        Assert.True(result.Succeeded);
        Assert.Equal("Submitted", result.Status);
        Assert.NotNull(result.Validation);
        Assert.True(result.Validation.IsValid);

        // Licensed capacity is written back to the rooms when the application is accepted.
        var application = await client.GetFromJsonAsync<LicenceApplicationDetails>(
            $"/api/licence-applications/{SampleApplicationId}");

        Assert.NotNull(application);
        Assert.Equal("Submitted", application.Status);
        Assert.NotNull(application.SubmittedAtUtc);
        Assert.All(application.Rooms, r => Assert.NotNull(r.LicensedCapacity));

        // A submitted application cannot be submitted again.
        var second = await client.PostAsync(
            $"/api/licence-applications/{SampleApplicationId}/submit",
            content: null);

        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task Submit_UnknownApplication_Returns404()
    {
        var response = await client.PostAsync(
            $"/api/licence-applications/{Guid.NewGuid()}/submit",
            content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
