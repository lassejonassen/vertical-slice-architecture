using System.Text.Json;

namespace VerticalSliceArchitecture.Api.IntegrationTests;

/// <summary>
/// The API serialises responses with ASP.NET Core's camelCase web defaults; case-insensitive
/// matching lets tests deserialise into PascalCase records without fighting the naming policy.
/// </summary>
internal static class TestJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };
}
