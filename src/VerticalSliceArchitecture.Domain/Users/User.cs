using VerticalSliceArchitecture.Domain.Users.Events;
using VerticalSliceArchitecture.SharedKernel.Abstractions;
using VerticalSliceArchitecture.SharedKernel.Domain;
using VerticalSliceArchitecture.SharedKernel.Results;

namespace VerticalSliceArchitecture.Domain.Users;

/// <summary>
/// The local projection of an authenticated principal.
/// <para>
/// Deliberately slim. The identity provider owns authentication, credentials, group membership and
/// most profile data; duplicating that here creates two sources of truth that drift. This aggregate
/// exists only so the rest of the domain has a stable <see cref="UserId"/> to hang foreign keys off,
/// and so audit trails survive a user being renamed or removed upstream.
/// </para>
/// <para>
/// Users are created just-in-time on first authenticated request rather than by an admin flow —
/// see the <c>GetCurrentUser</c> slice.
/// </para>
/// </summary>
public sealed class User : AggregateRoot<UserId>, IAuditable
{
    private User(
        UserId id,
        ExternalIdentity identity,
        string displayName,
        string? email,
        DateTimeOffset nowUtc) : base(id)
    {
        Identity = identity;
        DisplayName = displayName;
        Email = email;
        LastSeenOnUtc = nowUtc;
    }

    /// <summary>Required by EF Core. Do not use.</summary>
    private User()
    {
    }

    public ExternalIdentity Identity { get; private set; } = null!;

    public string DisplayName { get; private set; } = string.Empty;

    public string? Email { get; private set; }

    public DateTimeOffset LastSeenOnUtc { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTimeOffset? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }

    /// <summary>Creates the local record for a principal seen for the first time.</summary>
    public static Result<User> Provision(
        ExternalIdentity identity,
        string? displayName,
        string? email,
        DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(identity.Issuer) || string.IsNullOrWhiteSpace(identity.Subject))
        {
            return Result.Failure<User>(UserErrors.ExternalIdentityMissing);
        }

        // Fall back to the subject rather than rejecting: some providers issue tokens without a
        // name claim, and refusing to provision would lock the caller out entirely.
        string resolvedName = string.IsNullOrWhiteSpace(displayName)
            ? identity.Subject
            : displayName.Trim();

        var user = new User(UserId.New(), identity, resolvedName, Normalise(email), nowUtc);

        user.Raise(new UserProvisioned(user.Id, identity, nowUtc));

        return user;
    }

    /// <summary>
    /// Refreshes the local copy from the current token. Returns whether anything actually changed,
    /// so the caller can skip a write on the overwhelmingly common no-op path.
    /// </summary>
    public bool SyncFromClaims(string? displayName, string? email, DateTimeOffset nowUtc)
    {
        bool changed = false;

        if (!string.IsNullOrWhiteSpace(displayName) && displayName.Trim() != DisplayName)
        {
            DisplayName = displayName.Trim();
            changed = true;
        }

        string? normalisedEmail = Normalise(email);

        if (normalisedEmail is not null && normalisedEmail != Email)
        {
            Email = normalisedEmail;
            changed = true;
        }

        // Throttle so that a chatty client does not turn every request into an UPDATE.
        if (nowUtc - LastSeenOnUtc > TimeSpan.FromHours(1))
        {
            LastSeenOnUtc = nowUtc;
            changed = true;
        }

        return changed;
    }

    private static string? Normalise(string? email) =>
        string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
}
