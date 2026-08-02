using VerticalSliceArchitecture.Domain.Clients;
using VerticalSliceArchitecture.Domain.Clients.Events;
using VerticalSliceArchitecture.SharedKernel.Domain;

namespace VerticalSliceArchitecture.Api.Features.Clients;

/// <summary>
/// Example domain event handler. Handlers live in the feature folder of whatever reacts to the
/// event, not next to the event itself — that is what lets a second bounded context subscribe
/// later without touching the publisher.
/// <para>
/// Remember these run after the transaction commits and their failures are swallowed. Do not put
/// anything the operation depends on in here.
/// </para>
/// </summary>
internal sealed partial class ClientRegisteredLogger(ILogger<ClientRegisteredLogger> logger)
    : IDomainEventHandler<ClientRegistered>
{
    public Task HandleAsync(ClientRegistered domainEvent, CancellationToken cancellationToken = default)
    {
        LogClientRegistered(logger, domainEvent.ClientId, domainEvent.CompanyName, domainEvent.OccurredOnUtc);

        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Client {ClientId} ({CompanyName}) registered at {OccurredOnUtc}")]
    private static partial void LogClientRegistered(ILogger logger, ClientId clientId, string companyName, DateTimeOffset occurredOnUtc);
}
