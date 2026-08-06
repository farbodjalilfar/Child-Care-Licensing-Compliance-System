using ChildCareLicensing.Domain.Common;
using ChildCareLicensing.Domain.Enums;

namespace ChildCareLicensing.Domain.Entities;

public class LicenceApplication : Entity
{
    public Guid FacilityId { get; set; }

    public Facility Facility { get; set; } = null!;

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Draft;

    public DateTime? SubmittedAtUtc { get; set; }

    public DateTime? ReviewedAtUtc { get; set; }

    public string? ReviewerNotes { get; set; }

    public ICollection<ApplicationStatusHistory> StatusHistory { get; set; } = [];

    public Licence? Licence { get; set; }
}
