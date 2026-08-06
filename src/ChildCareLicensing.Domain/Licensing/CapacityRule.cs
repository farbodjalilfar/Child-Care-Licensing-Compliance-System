using ChildCareLicensing.Domain.Enums;

namespace ChildCareLicensing.Domain.Licensing;

/// <summary>
/// Simplified Ontario-style capacity limits used for portfolio demonstration.
/// Values are inspired by age-group rules under the Child Care and Early Years Act.
/// </summary>
public static class CapacityRule
{
    public static AgeGroupCapacity Infant { get; } = new(AgeGroup.Infant, StaffToChildRatio: 3, MaxGroupSize: 12, MinSqMetersPerChild: 2.5m);

    public static AgeGroupCapacity Toddler { get; } = new(AgeGroup.Toddler, StaffToChildRatio: 5, MaxGroupSize: 15, MinSqMetersPerChild: 2.5m);

    public static AgeGroupCapacity Preschool { get; } = new(AgeGroup.Preschool, StaffToChildRatio: 8, MaxGroupSize: 16, MinSqMetersPerChild: 2.8m);

    public static AgeGroupCapacity SchoolAge { get; } = new(AgeGroup.SchoolAge, StaffToChildRatio: 15, MaxGroupSize: 26, MinSqMetersPerChild: 2.8m);

    public static AgeGroupCapacity For(AgeGroup ageGroup) => ageGroup switch
    {
        AgeGroup.Infant => Infant,
        AgeGroup.Toddler => Toddler,
        AgeGroup.Preschool => Preschool,
        AgeGroup.SchoolAge => SchoolAge,
        _ => throw new ArgumentOutOfRangeException(nameof(ageGroup), ageGroup, "Unknown age group.")
    };
}

public readonly record struct AgeGroupCapacity(
    AgeGroup AgeGroup,
    int StaffToChildRatio,
    int MaxGroupSize,
    decimal MinSqMetersPerChild);
