using VerticalSliceArchitecture.SharedKernel.Domain;

namespace VerticalSliceArchitecture.Domain.Users.Events;

public sealed record UserProvisioned(UserId UserId, ExternalIdentity Identity, DateTimeOffset OccurredOnUtc)
    : DomainEvent(OccurredOnUtc);
