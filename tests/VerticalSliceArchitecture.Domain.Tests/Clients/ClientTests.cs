using Shouldly;
using VerticalSliceArchitecture.Domain.Clients;
using VerticalSliceArchitecture.Domain.Clients.Events;
using VerticalSliceArchitecture.SharedKernel.Results;

namespace VerticalSliceArchitecture.Domain.Tests.Clients;

public class ClientTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Register_WithValidData_ReturnsActiveClientAndRaisesClientRegistered()
    {
        Result<Client> result = Client.Register("Acme Corp", "contact@acme.test", Now);

        result.IsSuccess.ShouldBeTrue();
        Client client = result.Value;
        client.Name.Value.ShouldBe("Acme Corp");
        client.ContactEmail.Value.ShouldBe("contact@acme.test");
        client.Status.ShouldBe(ClientStatus.Active);
        client.IsActive.ShouldBeTrue();
        client.RegisteredOnUtc.ShouldBe(Now);
        client.DeactivatedOnUtc.ShouldBeNull();

        ClientRegistered raised = client.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<ClientRegistered>();
        raised.ClientId.ShouldBe(client.Id);
        raised.CompanyName.ShouldBe("Acme Corp");
        raised.OccurredOnUtc.ShouldBe(Now);
    }

    [Fact]
    public void Register_WithBlankCompanyNameAndInvalidEmail_ReportsBothFailuresAtOnce()
    {
        Result<Client> result = Client.Register(string.Empty, "not-an-email", Now);

        result.IsFailure.ShouldBeTrue();
        ValidationError validationError = result.Error.ShouldBeOfType<ValidationError>();
        validationError.Errors.ShouldContain(ClientErrors.CompanyNameEmpty);
        validationError.Errors.ShouldContain(ClientErrors.EmailInvalid);
    }

    [Fact]
    public void ChangeContactEmail_WithNewValue_UpdatesEmailAndRaisesEvent()
    {
        Client client = Client.Register("Acme Corp", "old@acme.test", Now).Value;
        client.ClearDomainEvents();

        Result result = client.ChangeContactEmail("new@acme.test", Now.AddDays(1));

        result.IsSuccess.ShouldBeTrue();
        client.ContactEmail.Value.ShouldBe("new@acme.test");
        ClientContactEmailChanged raised =
            client.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<ClientContactEmailChanged>();
        raised.PreviousEmail.ShouldBe("old@acme.test");
        raised.NewEmail.ShouldBe("new@acme.test");
    }

    [Fact]
    public void ChangeContactEmail_WithSameValue_IsIdempotentAndRaisesNoEvent()
    {
        Client client = Client.Register("Acme Corp", "same@acme.test", Now).Value;
        client.ClearDomainEvents();

        Result result = client.ChangeContactEmail("SAME@acme.test", Now.AddDays(1));

        result.IsSuccess.ShouldBeTrue();
        client.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void ChangeContactEmail_WhenInactive_ReturnsInactiveError()
    {
        Client client = Client.Register("Acme Corp", "contact@acme.test", Now).Value;
        client.Deactivate(Now.AddDays(1));

        Result result = client.ChangeContactEmail("new@acme.test", Now.AddDays(2));

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ClientErrors.Inactive);
    }

    [Fact]
    public void ChangeContactEmail_WithInvalidValue_ReturnsValidationErrorWithoutMutatingState()
    {
        Client client = Client.Register("Acme Corp", "contact@acme.test", Now).Value;

        Result result = client.ChangeContactEmail("not-an-email", Now.AddDays(1));

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ClientErrors.EmailInvalid);
        client.ContactEmail.Value.ShouldBe("contact@acme.test");
    }

    [Fact]
    public void Rename_WithValidName_UpdatesNameWithoutRaisingAnEvent()
    {
        Client client = Client.Register("Acme Corp", "contact@acme.test", Now).Value;
        client.ClearDomainEvents();

        Result result = client.Rename("Acme Corporation");

        result.IsSuccess.ShouldBeTrue();
        client.Name.Value.ShouldBe("Acme Corporation");
        client.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Rename_WhenInactive_ReturnsInactiveError()
    {
        Client client = Client.Register("Acme Corp", "contact@acme.test", Now).Value;
        client.Deactivate(Now.AddDays(1));

        Result result = client.Rename("New Name");

        result.Error.ShouldBe(ClientErrors.Inactive);
    }

    [Fact]
    public void Deactivate_WhenActive_TransitionsToInactiveAndRaisesEvent()
    {
        Client client = Client.Register("Acme Corp", "contact@acme.test", Now).Value;
        client.ClearDomainEvents();

        Result result = client.Deactivate(Now.AddDays(1));

        result.IsSuccess.ShouldBeTrue();
        client.Status.ShouldBe(ClientStatus.Inactive);
        client.IsActive.ShouldBeFalse();
        client.DeactivatedOnUtc.ShouldBe(Now.AddDays(1));
        client.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<ClientDeactivated>();
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_ReturnsAlreadyInactiveError()
    {
        Client client = Client.Register("Acme Corp", "contact@acme.test", Now).Value;
        client.Deactivate(Now.AddDays(1));

        Result result = client.Deactivate(Now.AddDays(2));

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ClientErrors.AlreadyInactive);
    }
}
