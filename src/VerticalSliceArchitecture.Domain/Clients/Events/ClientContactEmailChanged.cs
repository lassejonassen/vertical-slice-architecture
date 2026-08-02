using VerticalSliceArchitecture.SharedKernel.Domain;

namespace VerticalSliceArchitecture.Domain.Clients.Events;

public sealed record ClientContactEmailChanged(
    ClientId ClientId,
    string PreviousEmail,
    string NewEmail,
    DateTimeOffset OccurredOnUtc) : DomainEvent(OccurredOnUtc);
