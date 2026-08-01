using System.Text.Json;

namespace VerticalSliceArchitecture.Api.IntegrationTests;

internal static class TestJson
{
	// Matches ASP.NET Core's own default serializer settings (camelCase, case-insensitive)
	// so response bodies deserialize cleanly into the PascalCase record types below.
	public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}

internal sealed record ProblemResponse(string? Title, string? Detail, int? Status);
