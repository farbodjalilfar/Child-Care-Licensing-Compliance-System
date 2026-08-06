namespace ChildCareLicensing.Domain.Licensing;

public static class CapacityRulesEngine
{
    public static RoomCapacityValidationResult ValidateRoom(RoomCapacityRequest room)
    {
        var rule = CapacityRule.For(room.AgeGroup);
        var issues = new List<string>();

        if (room.FloorAreaSqM <= 0)
        {
            issues.Add("Floor area must be greater than zero.");
        }

        if (room.ProposedCapacity <= 0)
        {
            issues.Add("Proposed capacity must be at least one child.");
        }

        var maxByFloorArea = room.FloorAreaSqM > 0
            ? (int)Math.Floor(room.FloorAreaSqM / rule.MinSqMetersPerChild)
            : 0;

        var maxByGroupSize = rule.MaxGroupSize;
        var maxAllowed = Math.Min(maxByFloorArea, maxByGroupSize);

        if (room.ProposedCapacity > maxByFloorArea)
        {
            issues.Add(
                $"Proposed capacity of {room.ProposedCapacity} exceeds the floor-area limit of {maxByFloorArea} " +
                $"({rule.MinSqMetersPerChild} sq m per child in {room.AgeGroup}).");
        }

        if (room.ProposedCapacity > maxByGroupSize)
        {
            issues.Add(
                $"Proposed capacity of {room.ProposedCapacity} exceeds the maximum group size of {maxByGroupSize} " +
                $"for {room.AgeGroup}.");
        }

        var isValid = issues.Count == 0;
        var licensedCapacity = isValid ? room.ProposedCapacity : 0;
        var requiredStaff = licensedCapacity > 0
            ? (int)Math.Ceiling(licensedCapacity / (double)rule.StaffToChildRatio)
            : 0;

        return new RoomCapacityValidationResult(
            room.RoomName,
            room.AgeGroup,
            room.ProposedCapacity,
            maxByFloorArea,
            maxByGroupSize,
            maxAllowed,
            licensedCapacity,
            requiredStaff,
            isValid,
            issues);
    }

    public static FacilityCapacityValidationResult ValidateFacility(IEnumerable<RoomCapacityRequest> rooms)
    {
        var results = rooms.Select(ValidateRoom).ToList();
        return new FacilityCapacityValidationResult(
            results.All(r => r.IsValid),
            results);
    }
}
