using System;
using System.Collections.Generic;
using System.Text;
using VerticalSliceArchitecture.Domain.Clients.Events;
using VerticalSliceArchitecture.SharedKernel.Abstractions;
using VerticalSliceArchitecture.SharedKernel.Domain;
using VerticalSliceArchitecture.SharedKernel.Results;

namespace VerticalSliceArchitecture.Domain.Clients;

/// <summary>
/// A customer of the business. Reference implementation of the aggregate conventions used
/// throughout this template:
/// <list type="bullet">
///   <item>private setters — state changes only through intention-revealing methods</item>
///   <item>a static factory returning <see cref="Result{T}"/> instead of a throwing constructor</item>
///   <item>every mutator returns <see cref="Result"/> and guards its own invariants</item>
///   <item>timestamps are passed in, never read from the ambient clock</item>
///   <item>uniqueness across aggregates is checked by the handler, not here (see notes below)</item>
/// </list>
/// </summary>
public sealed class Client : AggregateRoot<ClientId>, IAuditable
{
    private Client(
        ClientId id,
        CompanyName name,
        EmailAddress contactEmail,
        DateTimeOffset registeredOnUtc) : base(id)
    {
        Name = name;
        ContactEmail = contactEmail;
        Status = ClientStatus.Active;
        RegisteredOnUtc = registeredOnUtc;
    }

    /// <summary>Required by EF Core. Do not use.</summary>
    private Client()
    {
    }

    public CompanyName Name { get; private set; }

    public EmailAddress ContactEmail { get; private set; }

    public ClientStatus Status { get; private set; }

    public DateTimeOffset RegisteredOnUtc { get; private set; }

    public DateTimeOffset? DeactivatedOnUtc { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTimeOffset? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }

    public bool IsActive => Status == ClientStatus.Active;

    /// <summary>
    /// Registers a new client.
    /// <para>
    /// Note what is <em>not</em> checked here: that the email is unique across all clients. That is
    /// a set-level rule, not an aggregate invariant — one <see cref="Client"/> instance cannot see
    /// the others. It is enforced by the handler (which queries) plus a unique index (which is the
    /// real guarantee under concurrency). Trying to pull it in here would require handing the
    /// aggregate a repository, which is the usual first step towards an anaemic model.
    /// </para>
    /// </summary>
    public static Result<Client> Register(
        string? companyName,
        string? contactEmail,
        DateTimeOffset nowUtc)
    {
        Result<CompanyName> name = CompanyName.Create(companyName);
        Result<EmailAddress> email = EmailAddress.Create(contactEmail);

        // Report both problems at once rather than making the caller fix them one at a time.
        Result validation = Result.AllOrValidationError(name, email);

        if (validation.IsFailure)
        {
            return Result.Failure<Client>(validation.Error);
        }

        var client = new Client(ClientId.New(), name.Value, email.Value, nowUtc);

        client.Raise(new ClientRegistered(client.Id, client.Name.Value, nowUtc));

        return client;
    }

    public Result ChangeContactEmail(string? newEmail, DateTimeOffset nowUtc)
    {
        if (!IsActive)
        {
            return ClientErrors.Inactive;
        }

        Result<EmailAddress> email = EmailAddress.Create(newEmail);

        if (email.IsFailure)
        {
            return Result.Failure(email.Error);
        }

        if (email.Value == ContactEmail)
        {
            // Idempotent: re-issuing the same value is a no-op, not an error.
            return Result.Success();
        }

        EmailAddress previous = ContactEmail;
        ContactEmail = email.Value;

        Raise(new ClientContactEmailChanged(Id, previous.Value, ContactEmail.Value, nowUtc));

        return Result.Success();
    }

    public Result Rename(string? newName)
    {
        if (!IsActive)
        {
            return ClientErrors.Inactive;
        }

        Result<CompanyName> name = CompanyName.Create(newName);

        if (name.IsFailure)
        {
            return Result.Failure(name.Error);
        }

        Name = name.Value;

        return Result.Success();
    }

    public Result Deactivate(DateTimeOffset nowUtc)
    {
        if (Status == ClientStatus.Inactive)
        {
            return ClientErrors.AlreadyInactive;
        }

        Status = ClientStatus.Inactive;
        DeactivatedOnUtc = nowUtc;

        Raise(new ClientDeactivated(Id, nowUtc));

        return Result.Success();
    }
}
