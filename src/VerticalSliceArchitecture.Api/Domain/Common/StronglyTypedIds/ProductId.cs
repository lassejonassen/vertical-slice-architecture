namespace VerticalSliceArchitecture.Api.Domain.Common.StronglyTypedIds;

public readonly record struct ProductId(Guid Value)
{
	public static ProductId New() => new(Guid.NewGuid());
}