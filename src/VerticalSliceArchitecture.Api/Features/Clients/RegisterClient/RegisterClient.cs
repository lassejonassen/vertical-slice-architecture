using FluentValidation;
using VerticalSliceArchitecture.Api.Infrastructure.Messaging;
using VerticalSliceArchitecture.Api.Infrastructure.Observability;
using VerticalSliceArchitecture.Domain.Clients;
using VerticalSliceArchitecture.SharedKernel.Abstractions;
using VerticalSliceArchitecture.SharedKernel.Results;

namespace VerticalSliceArchitecture.Api.Features.Clients.RegisterClient;

/// <summary>
/// The whole slice in one file: request contract, validator, command, handler and response.
/// <para>
/// Splitting these across five files gives you five things to open to understand one behaviour.
/// A slice is small by design, and when it stops being small that is a signal it should be split
/// into two slices — not spread thinner across more folders.
/// </para>
/// </summary>
public sealed record RegisterClientRequest(string CompanyName, string ContactEmail);

public sealed record RegisterClientResponse(Guid Id, string CompanyName, string ContactEmail);

/// <summary>
/// Shape validation only. That the email is well-formed is checked here <em>and</em> in
/// <c>EmailAddress</c> — the duplication is intentional: this one produces a helpful 400 keyed to
/// the request field, the domain one guarantees the invariant no matter who calls it.
/// </summary>
public sealed class RegisterClientValidator : AbstractValidator<RegisterClientRequest>
{
    public RegisterClientValidator()
    {
        RuleFor(request => request.CompanyName)
            .NotEmpty()
            .MaximumLength(CompanyName.MaxLength);

        RuleFor(request => request.ContactEmail)
            .NotEmpty()
            .MaximumLength(EmailAddress.MaxLength)
            .EmailAddress();
    }
}

public sealed record RegisterClientCommand(string CompanyName, string ContactEmail)
    : ICommand<RegisterClientResponse>;

internal sealed class RegisterClientHandler(
    IClientRepository clients,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<RegisterClientCommand, RegisterClientResponse>
{
    public async Task<Result<RegisterClientResponse>> HandleAsync(
        RegisterClientCommand command,
        CancellationToken cancellationToken = default)
    {
        Result<EmailAddress> email = EmailAddress.Create(command.ContactEmail);

        if (email.IsFailure)
        {
            return Result.Failure<RegisterClientResponse>(email.Error);
        }

        // A cross-aggregate rule, so it lives here rather than in Client. The unique index is what
        // actually enforces it under concurrency; this check exists to return a 409 with a useful
        // message instead of a 500 from a constraint violation.
        if (await clients.ExistsWithEmailAsync(email.Value, cancellationToken: cancellationToken))
        {
            return Result.Failure<RegisterClientResponse>(ClientErrors.EmailAlreadyInUse);
        }

        Result<Client> client = Client.Register(
            command.CompanyName,
            command.ContactEmail,
            dateTimeProvider.UtcNow);

        if (client.IsFailure)
        {
            return Result.Failure<RegisterClientResponse>(client.Error);
        }

        clients.Add(client.Value);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        DiagnosticsConstants.ClientsRegistered.Add(1);

        return new RegisterClientResponse(
            client.Value.Id.Value,
            client.Value.Name.Value,
            client.Value.ContactEmail.Value);
    }
}
