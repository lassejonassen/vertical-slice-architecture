using Microsoft.EntityFrameworkCore;
using VerticalSliceArchitecture.Domain.Users;
using VerticalSliceArchitecture.SharedKernel.Abstractions;
using VerticalSliceArchitecture.SharedKernel.Results;

namespace VerticalSliceArchitecture.Api.Infrastructure.Security;

/// <summary>
/// Maps the authenticated principal onto a local <see cref="User"/>, creating one on first sight.
/// </summary>
public interface IUserProvisioningService
{
    public Task<Result<User>> GetOrProvisionCurrentUserAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Just-in-time provisioning.
/// <para>
/// The alternative — an admin creating accounts up front, or a nightly sync from the directory —
/// means a user who was granted access five minutes ago cannot use the system, and it means
/// maintaining a copy of the directory that is always slightly wrong. JIT keeps the identity
/// provider authoritative: if the token validates, the user exists.
/// </para>
/// <para>
/// The race is real but benign. Two concurrent first requests can both find nothing and both
/// insert; the unique index on (issuer, subject) rejects the loser, and the retry below resolves
/// to the row the winner wrote.
/// </para>
/// </summary>
internal sealed partial class UserProvisioningService(
    ICurrentUser currentUser,
    IUserRepository users,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    ILogger<UserProvisioningService> logger) : IUserProvisioningService
{
    public async Task<Result<User>> GetOrProvisionCurrentUserAsync(
        CancellationToken cancellationToken = default)
    {
        Result<ExternalIdentity> identity = currentUser.Identity;

        if (identity.IsFailure)
        {
            return Result.Failure<User>(identity.Error);
        }

        User? existing = await users.GetByExternalIdentityAsync(identity.Value, cancellationToken);

        if (existing is not null)
        {
            if (existing.SyncFromClaims(currentUser.DisplayName, currentUser.Email, dateTimeProvider.UtcNow))
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return existing;
        }

        Result<User> provisioned = User.Provision(
            identity.Value,
            currentUser.DisplayName,
            currentUser.Email,
            dateTimeProvider.UtcNow);

        if (provisioned.IsFailure)
        {
            return provisioned;
        }

        users.Add(provisioned.Value);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);

            LogProvisionedUser(logger, provisioned.Value.Id, identity.Value.Issuer);

            return provisioned;
        }
        catch (DbUpdateException)
        {
            // Lost the race. The row now exists; read it back rather than failing the request.
            User? winner = await users.GetByExternalIdentityAsync(identity.Value, cancellationToken);

            return winner is not null
                ? Result.Success(winner)
                : Result.Failure<User>(UserErrors.NotFound);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Provisioned local user {UserId} for external identity {Issuer}")]
    private static partial void LogProvisionedUser(ILogger logger, UserId userId, string issuer);
}
