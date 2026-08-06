using ChildCareLicensing.Domain.Enums;

namespace ChildCareLicensing.Domain.Licensing;

public sealed record RoomCapacityRequest(
    string RoomName,
    AgeGroup AgeGroup,
    decimal FloorAreaSqM,
    int ProposedCapacity);

public sealed record RoomCapacityValidationResult(
    string RoomName,
    AgeGroup AgeGroup,
    int ProposedCapacity,
    int MaxCapacityByFloorArea,
    int MaxCapacityByGroupSize,
    int MaxAllowedCapacity,
    int LicensedCapacity,
    int RequiredStaff,
    bool IsValid,
    IReadOnlyList<string> Issues);

public sealed record FacilityCapacityValidationResult(
    bool IsValid,
    IReadOnlyList<RoomCapacityValidationResult> Rooms);
