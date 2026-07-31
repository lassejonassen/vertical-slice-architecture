namespace VerticalSliceArchitecture.Api.Domain.Common.StronglyTypedIds;

public readonly record struct OrderId(Guid Value)
{
	public static OrderId New() => new(Guid.NewGuid());
}