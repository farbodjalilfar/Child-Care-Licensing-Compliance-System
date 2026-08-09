using ChildCareLicensing.Domain.Common;
using ChildCareLicensing.Domain.Enums;

namespace ChildCareLicensing.Domain.Entities;

/// <summary>
/// A person who signs in. A production deployment would federate with the ministry's
/// identity provider instead of storing credentials; this table exists so the demo is
/// self-contained.
/// </summary>
public class User : Entity
{
    public required string Email { get; set; }

    public required string DisplayName { get; set; }

    public required string PasswordHash { get; set; }

    public UserRole Role { get; set; }

    /// <summary>Set for operator accounts, which only see their own centres.</summary>
    public Guid? OperatorId { get; set; }

    public Operator? Operator { get; set; }
}
