using ChildCareLicensing.Domain.Enums;
using ChildCareLicensing.Domain.Licensing;

namespace ChildCareLicensing.UnitTests.Licensing;

public class CapacityRulesEngineTests
{
    [Fact]
    public void ValidateRoom_InfantWithinLimits_IsValid()
    {
        var result = CapacityRulesEngine.ValidateRoom(new RoomCapacityRequest(
            "Infant Room A",
            AgeGroup.Infant,
            FloorAreaSqM: 45,
            ProposedCapacity: 10));

        Assert.True(result.IsValid);
        Assert.Equal(10, result.LicensedCapacity);
        Assert.Equal(18, result.MaxCapacityByFloorArea);
        Assert.Equal(12, result.MaxCapacityByGroupSize);
        Assert.Equal(4, result.RequiredStaff);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void ValidateRoom_InfantExceedsFloorArea_IsInvalid()
    {
        var result = CapacityRulesEngine.ValidateRoom(new RoomCapacityRequest(
            "Infant Room A",
            AgeGroup.Infant,
            FloorAreaSqM: 45,
            ProposedCapacity: 20));

        Assert.False(result.IsValid);
        Assert.Equal(0, result.LicensedCapacity);
        Assert.Contains(result.Issues, issue => issue.Contains("floor-area limit"));
    }

    [Fact]
    public void ValidateRoom_InfantExceedsGroupSize_IsInvalid()
    {
        var result = CapacityRulesEngine.ValidateRoom(new RoomCapacityRequest(
            "Infant Room A",
            AgeGroup.Infant,
            FloorAreaSqM: 100,
            ProposedCapacity: 15));

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Contains("maximum group size"));
    }

    [Fact]
    public void ValidateRoom_ToddlerCalculatesStaffRequirement()
    {
        var result = CapacityRulesEngine.ValidateRoom(new RoomCapacityRequest(
            "Toddler Room B",
            AgeGroup.Toddler,
            FloorAreaSqM: 55,
            ProposedCapacity: 15));

        Assert.True(result.IsValid);
        Assert.Equal(3, result.RequiredStaff);
    }

    [Fact]
    public void ValidateRoom_ZeroFloorArea_IsInvalid()
    {
        var result = CapacityRulesEngine.ValidateRoom(new RoomCapacityRequest(
            "Bad Room",
            AgeGroup.Preschool,
            FloorAreaSqM: 0,
            ProposedCapacity: 5));

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Contains("Floor area"));
    }

    [Fact]
    public void ValidateFacility_OneInvalidRoomMakesFacilityInvalid()
    {
        var result = CapacityRulesEngine.ValidateFacility(
        [
            new RoomCapacityRequest("Infant Room A", AgeGroup.Infant, 45, 10),
            new RoomCapacityRequest("Toddler Room B", AgeGroup.Toddler, 55, 20)
        ]);

        Assert.False(result.IsValid);
        Assert.Equal(2, result.Rooms.Count);
        Assert.True(result.Rooms.First(r => r.RoomName == "Infant Room A").IsValid);
        Assert.False(result.Rooms.First(r => r.RoomName == "Toddler Room B").IsValid);
    }

    [Fact]
    public void ValidateFacility_AllRoomsValid_IsValid()
    {
        var result = CapacityRulesEngine.ValidateFacility(
        [
            new RoomCapacityRequest("Infant Room A", AgeGroup.Infant, 45, 10),
            new RoomCapacityRequest("Toddler Room B", AgeGroup.Toddler, 55, 15)
        ]);

        Assert.True(result.IsValid);
    }
}
