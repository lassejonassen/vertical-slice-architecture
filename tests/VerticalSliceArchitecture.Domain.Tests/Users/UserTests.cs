using Shouldly;
using VerticalSliceArchitecture.Domain.Users;
using VerticalSliceArchitecture.Domain.Users.Events;
using VerticalSliceArchitecture.SharedKernel.Results;

namespace VerticalSliceArchitecture.Domain.Tests.Users;

public class UserTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly ExternalIdentity Identity = new("entra:tenant-1", "object-id-1");

    [Fact]
    public void Provision_WithValidIdentity_CreatesUserAndRaisesUserProvisioned()
    {
        Result<User> result = User.Provision(Identity, "Ada Lovelace", "Ada@Example.test", Now);

        result.IsSuccess.ShouldBeTrue();
        User user = result.Value;
        user.Identity.ShouldBe(Identity);
        user.DisplayName.ShouldBe("Ada Lovelace");
        user.Email.ShouldBe("ada@example.test");
        user.LastSeenOnUtc.ShouldBe(Now);

        UserProvisioned raised = user.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<UserProvisioned>();
        raised.UserId.ShouldBe(user.Id);
        raised.Identity.ShouldBe(Identity);
    }

    [Fact]
    public void Provision_WithoutDisplayName_FallsBackToTheSubjectClaim()
    {
        Result<User> result = User.Provision(Identity, displayName: null, email: null, Now);

        result.IsSuccess.ShouldBeTrue();
        result.Value.DisplayName.ShouldBe(Identity.Subject);
        result.Value.Email.ShouldBeNull();
    }

    [Theory]
    [InlineData("", "subject")]
    [InlineData("issuer", "")]
    public void Provision_WithMissingIssuerOrSubject_ReturnsExternalIdentityMissing(string issuer, string subject)
    {
        Result<User> result = User.Provision(new ExternalIdentity(issuer, subject), "Ada", null, Now);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(UserErrors.ExternalIdentityMissing);
    }

    [Fact]
    public void SyncFromClaims_WithChangedDisplayNameAndEmail_UpdatesBothAndReturnsTrue()
    {
        User user = User.Provision(Identity, "Ada Lovelace", "ada@example.test", Now).Value;

        bool changed = user.SyncFromClaims("Ada, Countess of Lovelace", "new@example.test", Now.AddMinutes(5));

        changed.ShouldBeTrue();
        user.DisplayName.ShouldBe("Ada, Countess of Lovelace");
        user.Email.ShouldBe("new@example.test");
    }

    [Fact]
    public void SyncFromClaims_WithSameValuesWithinTheThrottleWindow_ReturnsFalseAndRaisesNoEvent()
    {
        User user = User.Provision(Identity, "Ada Lovelace", "ada@example.test", Now).Value;
        user.ClearDomainEvents();

        bool changed = user.SyncFromClaims("Ada Lovelace", "ada@example.test", Now.AddMinutes(5));

        changed.ShouldBeFalse();
        user.LastSeenOnUtc.ShouldBe(Now);
        user.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void SyncFromClaims_AfterTheThrottleWindowWithNoOtherChanges_UpdatesLastSeenOnlyAndReturnsTrue()
    {
        User user = User.Provision(Identity, "Ada Lovelace", "ada@example.test", Now).Value;
        DateTimeOffset later = Now.AddHours(2);

        bool changed = user.SyncFromClaims("Ada Lovelace", "ada@example.test", later);

        changed.ShouldBeTrue();
        user.LastSeenOnUtc.ShouldBe(later);
    }

    [Fact]
    public void SyncFromClaims_NeverRaisesADomainEvent()
    {
        User user = User.Provision(Identity, "Ada Lovelace", "ada@example.test", Now).Value;
        user.ClearDomainEvents();

        user.SyncFromClaims("New Name", "new@example.test", Now.AddHours(2));

        user.DomainEvents.ShouldBeEmpty();
    }
}
