using System.Net;

namespace ChildCareLicensing.IntegrationTests;

/// <summary>
/// The register is deliberately open to everyone; everything else needs an account, and an
/// account only reaches what its role allows.
/// </summary>
public class AuthorizationTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Theory]
    [InlineData("/api/facilities")]
    [InlineData("/api/reports/facility-compliance")]
    [InlineData("/api/review/licence-applications/queue")]
    public async Task ProtectedEndpoints_RejectAnonymousCallers(string path)
    {
        var response = await factory.CreateClient().GetAsync(path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/public/facilities?name=sunshine")]
    [InlineData("/health")]
    public async Task PublicEndpoints_StayOpen(string path)
    {
        var response = await factory.CreateClient().GetAsync(path);

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task ReviewQueue_IsClosedToOperators()
    {
        var client = factory.CreateClientAs(
            TestIdentities.OperatorRole,
            TestIdentities.SunshineOperatorId);

        var response = await client.GetAsync("/api/review/licence-applications/queue");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Inspector_CannotDecideApplications()
    {
        var client = factory.CreateClientAs(TestIdentities.InspectorRole);

        var response = await client.PostAsync(
            $"/api/review/licence-applications/{TestIdentities.SunshineApplicationId}/start-review",
            content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Operator_CannotOpenAnotherOperatorsApplication()
    {
        var client = factory.CreateClientAs(
            TestIdentities.OperatorRole,
            TestIdentities.MapleGroveOperatorId);

        var response = await client.GetAsync(
            $"/api/licence-applications/{TestIdentities.SunshineApplicationId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Operator_CannotSubmitAnotherOperatorsApplication()
    {
        var client = factory.CreateClientAs(
            TestIdentities.OperatorRole,
            TestIdentities.MapleGroveOperatorId);

        var response = await client.PostAsync(
            $"/api/licence-applications/{TestIdentities.SunshineApplicationId}/submit",
            content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
