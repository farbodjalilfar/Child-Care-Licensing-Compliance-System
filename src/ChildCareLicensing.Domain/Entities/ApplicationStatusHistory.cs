using ChildCareLicensing.Domain.Common;
using ChildCareLicensing.Domain.Enums;

namespace ChildCareLicensing.Domain.Entities;

public class ApplicationStatusHistory : Entity
{
    public Guid ApplicationId { get; set; }

    public LicenceApplication Application { get; set; } = null!;

    public ApplicationStatus? FromStatus { get; set; }

    public ApplicationStatus ToStatus { get; set; }

    public DateTime ChangedAtUtc { get; set; }

    public required string ChangedBy { get; set; }

    public string? Notes { get; set; }
}
