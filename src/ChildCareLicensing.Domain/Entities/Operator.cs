using ChildCareLicensing.Domain.Common;

namespace ChildCareLicensing.Domain.Entities;

public class Operator : Entity
{
    public required string LegalName { get; set; }

    public required string ContactEmail { get; set; }

    public string? ContactPhone { get; set; }

    public ICollection<Facility> Facilities { get; set; } = [];
}
