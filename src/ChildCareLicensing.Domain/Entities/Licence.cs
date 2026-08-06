using ChildCareLicensing.Domain.Common;
using ChildCareLicensing.Domain.Enums;

namespace ChildCareLicensing.Domain.Entities;

public class Licence : Entity
{
    public Guid FacilityId { get; set; }

    public Facility Facility { get; set; } = null!;

    public Guid ApplicationId { get; set; }

    public LicenceApplication Application { get; set; } = null!;

    public required string LicenceNumber { get; set; }

    public DateTime IssuedAtUtc { get; set; }

    public DateTime ExpiresAtUtc { get; set; }

    public LicenceStatus Status { get; set; } = LicenceStatus.Active;
}
