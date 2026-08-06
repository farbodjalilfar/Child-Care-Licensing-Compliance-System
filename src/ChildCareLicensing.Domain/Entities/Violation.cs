using ChildCareLicensing.Domain.Common;
using ChildCareLicensing.Domain.Enums;

namespace ChildCareLicensing.Domain.Entities;

public class Violation : Entity
{
    public Guid InspectionId { get; set; }

    public Inspection Inspection { get; set; } = null!;

    public required string Category { get; set; }

    public required string Description { get; set; }

    public ViolationSeverity Severity { get; set; }

    public ViolationStatus Status { get; set; } = ViolationStatus.Open;

    public DateTime RemediationDeadlineUtc { get; set; }

    public DateTime? RemediatedAtUtc { get; set; }
}
