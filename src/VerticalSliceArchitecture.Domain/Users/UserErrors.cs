using VerticalSliceArchitecture.SharedKernel.Results;

namespace VerticalSliceArchitecture.Domain.Users;

public static class UserErrors
{
    public static readonly Error NotFound =
        Error.NotFound("User.NotFound", "No user was found with the specified identifier.");

    public static readonly Error NotAuthenticated =
        Error.Unauthorized("User.NotAuthenticated", "The request is not authenticated.");

    public static readonly Error ExternalIdentityMissing =
        Error.Unauthorized(
            "User.ExternalIdentityMissing",
            "The access token does not contain the claims required to identify the caller.");

    public static readonly Error DisplayNameEmpty =
        Error.Validation("User.DisplayName.Empty", "Display name is required.");
}
