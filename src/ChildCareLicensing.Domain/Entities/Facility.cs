using ChildCareLicensing.Domain.Common;

namespace ChildCareLicensing.Domain.Entities;

public class Facility : Entity
{
    public Guid OperatorId { get; set; }

    public Operator Operator { get; set; } = null!;

    public required string Name { get; set; }

    public required string AddressLine1 { get; set; }

    public string? AddressLine2 { get; set; }

    public required string City { get; set; }

    public required string Province { get; set; }

    public required string PostalCode { get; set; }

    public ICollection<Room> Rooms { get; set; } = [];

    public ICollection<LicenceApplication> Applications { get; set; } = [];

    public ICollection<Licence> Licences { get; set; } = [];

    public ICollection<Inspection> Inspections { get; set; } = [];
}
