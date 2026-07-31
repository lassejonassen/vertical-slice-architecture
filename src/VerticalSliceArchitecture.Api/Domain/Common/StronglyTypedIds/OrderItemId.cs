namespace VerticalSliceArchitecture.Api.Domain.Common.StronglyTypedIds;

public readonly record struct OrderItemId(Guid Value)
{
	public static OrderItemId New() => new(Guid.NewGuid());
}
