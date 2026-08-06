using ChildCareLicensing.Domain.Common;
using ChildCareLicensing.Domain.Enums;

namespace ChildCareLicensing.Domain.Entities;

public class Room : Entity
{
    public Guid FacilityId { get; set; }

    public Facility Facility { get; set; } = null!;

    public required string Name { get; set; }

    public AgeGroup AgeGroup { get; set; }

    /// <summary>Floor area in square metres.</summary>
    public decimal FloorAreaSqM { get; set; }

    public int ProposedCapacity { get; set; }

    /// <summary>Capacity approved after ratio and floor-area rules are applied.</summary>
    public int? LicensedCapacity { get; set; }
}
