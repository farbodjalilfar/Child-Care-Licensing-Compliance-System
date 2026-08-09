using System.Net;
using System.Net.Http.Json;
using ChildCareLicensing.Application.Abstractions;
using ChildCareLicensing.Domain.Licensing;

namespace ChildCareLicensing.IntegrationTests;

public class LicenceApplicationQueryTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient client = factory.CreateClientAs(
        TestIdentities.OperatorRole,
        TestIdentities.SunshineOperatorId);

    [Fact]
    public async Task Get_ReturnsDraftApplicationWithRooms()
    {
        var application = await client.GetFromJsonAsync<LicenceApplicationDetails>(
            $"/api/licence-applications/{TestIdentities.SunshineApplicationId}");

        Assert.NotNull(application);
        Assert.Equal("Draft", application.Status);
        Assert.Equal("Sunshine Early Learning Centre", application.FacilityName);
        Assert.Equal(2, application.Rooms.Count);
    }

    [Fact]
    public async Task Get_UnknownApplication_Returns404()
    {
        var response = await client.GetAsync($"/api/licence-applications/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Validation_ReturnsPerRoomCapacityResults()
    {
        var validation = await client.GetFromJsonAsync<FacilityCapacityValidationResult>(
            $"/api/licence-applications/{TestIdentities.SunshineApplicationId}/validation");

        Assert.NotNull(validation);
        Assert.True(validation.IsValid);

        var infantRoom = Assert.Single(validation.Rooms, r => r.RoomName == "Infant Room A");
        Assert.Equal(10, infantRoom.LicensedCapacity);
        Assert.Equal(4, infantRoom.RequiredStaff);
        Assert.Empty(infantRoom.Issues);
    }

    [Fact]
    public async Task Validation_UnknownApplication_Returns404()
    {
        var response = await client.GetAsync(
            $"/api/licence-applications/{Guid.NewGuid()}/validation");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Facilities_ReturnsOnlyTheSignedInOperatorsCentres()
    {
        var body = await client.GetStringAsync("/api/facilities");

        Assert.Contains("Sunshine Early Learning Centre", body);
        Assert.DoesNotContain("Maple Grove Children's Centre", body);
    }

    [Fact]
    public async Task Facilities_ForMinistryStaff_ReturnsTheWholeRegister()
    {
        var ministry = factory.CreateClientAs(TestIdentities.ReviewerRole);

        var body = await ministry.GetStringAsync("/api/facilities");

        Assert.Contains("Sunshine Early Learning Centre", body);
        Assert.Contains("Maple Grove Children's Centre", body);
    }

    [Fact]
    public async Task Health_ReportsHealthy()
    {
        var response = await factory.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5000)]
    public async Task Reports_RejectInvalidLookbackWindow(int lookbackDays)
    {
        var ministry = factory.CreateClientAs(TestIdentities.InspectorRole);

        var response = await ministry.GetAsync(
            $"/api/reports/violations-by-category?lookbackDays={lookbackDays}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
