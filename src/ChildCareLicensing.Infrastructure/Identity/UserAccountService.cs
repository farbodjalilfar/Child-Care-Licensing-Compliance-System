using ChildCareLicensing.Application.Identity;
using ChildCareLicensing.Domain.Entities;
using ChildCareLicensing.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ChildCareLicensing.Infrastructure.Identity;

/// <summary>
/// Credentials are checked against the local Users table. A real deployment would federate
/// with the ministry's identity provider; this keeps the demo self-contained while still
/// using the framework's password hasher rather than anything hand-rolled.
/// </summary>
public sealed class UserAccountService(
    ApplicationDbContext dbContext,
    IPasswordHasher<User> passwordHasher) : IUserAccountService
{
    public async Task<UserAccount?> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        var normalised = email.Trim().ToLowerInvariant();

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == normalised, cancellationToken);

        if (user is null)
        {
            return null;
        }

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed)
        {
            return null;
        }

        return new UserAccount(user.Id, user.Email, user.DisplayName, user.Role, user.OperatorId);
    }
}
