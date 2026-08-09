using ChildCareLicensing.Domain.Enums;

namespace ChildCareLicensing.Application.Identity;

public sealed record UserAccount(
    Guid Id,
    string Email,
    string DisplayName,
    UserRole Role,
    Guid? OperatorId);

public interface IUserAccountService
{
    /// <summary>
    /// Returns the account when the credentials match, otherwise null. The caller should not
    /// distinguish between an unknown email and a wrong password when reporting failure.
    /// </summary>
    Task<UserAccount?> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);
}
