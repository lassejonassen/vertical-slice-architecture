using VerticalSliceArchitecture.SharedKernel.Domain;

namespace VerticalSliceArchitecture.Domain.Clients.Events;

public sealed record ClientRegistered(ClientId ClientId, string CompanyName, DateTimeOffset OccurredOnUtc)
    : DomainEvent(OccurredOnUtc);
