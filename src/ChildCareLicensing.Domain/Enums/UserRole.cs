namespace ChildCareLicensing.Domain.Enums;

public enum UserRole
{
    /// <summary>Applies to license the centres belonging to one operator.</summary>
    Operator = 1,

    /// <summary>Ministry staff who review applications and monitor compliance.</summary>
    Reviewer = 2,

    /// <summary>Ministry staff who carry out inspections.</summary>
    Inspector = 3
}
