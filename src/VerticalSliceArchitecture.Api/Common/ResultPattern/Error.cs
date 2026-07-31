namespace VerticalSliceArchitecture.Api.Common.ResultPattern;

public sealed record Error(string Code, string Description, ErrorType Type)
{
	public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);
	public static readonly Error NullValue = new("Error.NullValue", "Null Value", ErrorType.Failure);
}

public enum ErrorType
{
	Failure,
	Validation,
	NotFound,
	Conflict
}
