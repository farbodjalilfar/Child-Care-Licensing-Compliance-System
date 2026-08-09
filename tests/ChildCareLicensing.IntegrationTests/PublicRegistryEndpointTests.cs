using System.Net;
using System.Net.Http.Json;
using ChildCareLicensing.Application.PublicRegistry;

namespace ChildCareLicensing.IntegrationTests;

public class PublicRegistryEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task Search_ReturnsSeededFacilities()
    {
        var listings = await client.GetFromJsonAsync<List<PublicFacilityListing>>("/api/public/facilities");

        Assert.NotNull(listings);
        Assert.Contains(listings, f => f.Name == "Maple Grove Children's Centre");
        Assert.Contains(listings, f => f.LicenceStatus == "Unlicensed");
    }

    [Fact]
    public async Task Search_FiltersByCity()
    {
        var listings = await client.GetFromJsonAsync<List<PublicFacilityListing>>(
            "/api/public/facilities?city=Ottawa");

        Assert.NotNull(listings);
        Assert.NotEmpty(listings);
        Assert.All(listings, f => Assert.Equal("Ottawa", f.City));
    }

    [Fact]
    public async Task Search_FiltersByName()
    {
        var listings = await client.GetFromJsonAsync<List<PublicFacilityListing>>(
            "/api/public/facilities?name=Riverside");

        Assert.NotNull(listings);
        var facility = Assert.Single(listings);
        Assert.Equal("Riverside Kids Club", facility.Name);
        Assert.Equal("ON-CCL-005190", facility.LicenceNumber);
    }

    [Fact]
    public async Task Search_ReportsOpenViolations()
    {
        var listings = await client.GetFromJsonAsync<List<PublicFacilityListing>>(
            "/api/public/facilities?name=Riverside");

        var facility = Assert.Single(listings!);
        Assert.Equal(1, facility.OpenViolations);
    }

    [Fact]
    public async Task Get_ReturnsInspectionHistory()
    {
        var listings = await client.GetFromJsonAsync<List<PublicFacilityListing>>(
            "/api/public/facilities?name=Maple");
        var facilityId = Assert.Single(listings!).FacilityId;

        var detail = await client.GetFromJsonAsync<PublicFacilityDetail>(
            $"/api/public/facilities/{facilityId}");

        Assert.NotNull(detail);
        Assert.Equal(2, detail.Inspections.Count);
        Assert.Equal(30, detail.LicensedCapacity);
        Assert.True(
            detail.Inspections[0].InspectionDateUtc >= detail.Inspections[1].InspectionDateUtc,
            "Inspections should be returned newest first.");
    }

    [Fact]
    public async Task Get_UnknownFacility_Returns404()
    {
        var response = await client.GetAsync($"/api/public/facilities/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
