using Shouldly;
using VerticalSliceArchitecture.Domain.Clients;
using VerticalSliceArchitecture.SharedKernel.Results;

namespace VerticalSliceArchitecture.Domain.Tests.Clients;

public class EmailAddressTests
{
    [Fact]
    public void Create_WithValidAddress_TrimsAndNormalisesToLowerCase()
    {
        Result<EmailAddress> result = EmailAddress.Create("  Contact@ACME.test  ");

        result.IsSuccess.ShouldBeTrue();
        result.Value.Value.ShouldBe("contact@acme.test");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankValue_ReturnsEmailEmpty(string? input)
    {
        Result<EmailAddress> result = EmailAddress.Create(input);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ClientErrors.EmailEmpty);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing-at-sign.test")]
    public void Create_WithInvalidAddress_ReturnsEmailInvalid(string input)
    {
        Result<EmailAddress> result = EmailAddress.Create(input);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ClientErrors.EmailInvalid);
    }

    [Fact]
    public void Create_LongerThanMaxLength_ReturnsEmailTooLong()
    {
        string localPart = new('a', EmailAddress.MaxLength);
        string tooLong = $"{localPart}@acme.test";

        Result<EmailAddress> result = EmailAddress.Create(tooLong);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ClientErrors.EmailTooLong);
    }

    [Fact]
    public void Equality_IsCaseInsensitiveBecauseValuesAreNormalisedOnCreate()
    {
        EmailAddress first = EmailAddress.Create("Contact@Acme.test").Value;
        EmailAddress second = EmailAddress.Create("contact@acme.test").Value;

        first.ShouldBe(second);
    }
}
