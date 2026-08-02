using VerticalSliceArchitecture.SharedKernel.Domain;

namespace VerticalSliceArchitecture.Domain.Clients;

public readonly record struct ClientId(Guid Value) : IStronglyTypedId<ClientId>
{
    public static ClientId New() => new(Guid.CreateVersion7());

    public static ClientId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
