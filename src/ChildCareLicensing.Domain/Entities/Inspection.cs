using ChildCareLicensing.Domain.Common;

namespace ChildCareLicensing.Domain.Entities;

public class Inspection : Entity
{
    public Guid FacilityId { get; set; }

    public Facility Facility { get; set; } = null!;

    public DateTime InspectionDateUtc { get; set; }

    public required string InspectorName { get; set; }

    public string? Summary { get; set; }

    public ICollection<Violation> Violations { get; set; } = [];
}
