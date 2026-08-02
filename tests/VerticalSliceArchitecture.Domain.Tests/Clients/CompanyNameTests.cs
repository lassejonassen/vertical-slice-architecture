using Shouldly;
using VerticalSliceArchitecture.Domain.Clients;
using VerticalSliceArchitecture.SharedKernel.Results;

namespace VerticalSliceArchitecture.Domain.Tests.Clients;

public class CompanyNameTests
{
    [Theory]
    [InlineData("Acme Corp", "Acme Corp")]
    [InlineData("  Acme Corp  ", "Acme Corp")]
    public void Create_WithValidValue_TrimsAndSucceeds(string input, string expected)
    {
        Result<CompanyName> result = CompanyName.Create(input);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Value.ShouldBe(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankValue_ReturnsCompanyNameEmpty(string? input)
    {
        Result<CompanyName> result = CompanyName.Create(input);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ClientErrors.CompanyNameEmpty);
    }

    [Fact]
    public void Create_LongerThanMaxLength_ReturnsCompanyNameTooLong()
    {
        string tooLong = new('a', CompanyName.MaxLength + 1);

        Result<CompanyName> result = CompanyName.Create(tooLong);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ClientErrors.CompanyNameTooLong);
    }

    [Fact]
    public void Create_AtMaxLength_Succeeds()
    {
        string atMax = new('a', CompanyName.MaxLength);

        Result<CompanyName> result = CompanyName.Create(atMax);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ToString_ReturnsTheValue()
    {
        CompanyName name = CompanyName.Create("Acme Corp").Value;

        name.ToString().ShouldBe("Acme Corp");
    }
}
