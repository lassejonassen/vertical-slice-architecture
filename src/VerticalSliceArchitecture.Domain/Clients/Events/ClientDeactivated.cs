using VerticalSliceArchitecture.SharedKernel.Domain;

namespace VerticalSliceArchitecture.Domain.Clients.Events;

public sealed record ClientDeactivated(ClientId ClientId, DateTimeOffset OccurredOnUtc)
    : DomainEvent(OccurredOnUtc);
