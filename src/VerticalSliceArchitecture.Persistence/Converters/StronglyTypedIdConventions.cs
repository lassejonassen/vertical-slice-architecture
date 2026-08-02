using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using VerticalSliceArchitecture.Domain.Clients;
using VerticalSliceArchitecture.Domain.Users;

namespace VerticalSliceArchitecture.Persistence.Converters;

/// <summary>
/// Teaches EF Core to store strongly-typed IDs as plain GUID columns.
/// <para>
/// Each new ID type needs one line in <see cref="RegisterStronglyTypedIds"/>. That is a deliberate
/// trade: a source generator or assembly scan would avoid the repetition, but a forgotten
/// registration then fails at runtime with an opaque error instead of being obvious here.
/// </para>
/// </summary>
public static class StronglyTypedIdConventions
{
    public static ModelConfigurationBuilder RegisterStronglyTypedIds(this ModelConfigurationBuilder builder)
    {
        builder.Properties<ClientId>().HaveConversion<ClientIdConverter>();
        builder.Properties<UserId>().HaveConversion<UserIdConverter>();

        return builder;
    }

    private sealed class ClientIdConverter() : ValueConverter<ClientId, Guid>(
        id => id.Value,
        value => ClientId.From(value));

    private sealed class UserIdConverter() : ValueConverter<UserId, Guid>(
        id => id.Value,
        value => UserId.From(value));
}
