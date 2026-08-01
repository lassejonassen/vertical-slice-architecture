using VerticalSliceArchitecture.Api.Domain.Common.ValueObjects;

namespace VerticalSliceArchitecture.Api.Tests.Domain;

public class MoneyTests
{
	[Fact]
	public void Create_WithNegativeAmount_Throws()
	{
		var act = () => Money.Create(-1m);

		Assert.Throws<ArgumentException>(act);
	}

	[Fact]
	public void Create_WithValidAmount_DefaultsToUsd()
	{
		var money = Money.Create(10m);

		Assert.Equal(10m, money.Amount);
		Assert.Equal("USD", money.Currency);
	}

	[Fact]
	public void Zero_ReturnsZeroAmountWithGivenCurrency()
	{
		var money = Money.Zero("EUR");

		Assert.Equal(0m, money.Amount);
		Assert.Equal("EUR", money.Currency);
	}

	[Fact]
	public void Addition_SameCurrency_SumsAmounts()
	{
		var result = Money.Create(10m, "USD") + Money.Create(5m, "USD");

		Assert.Equal(15m, result.Amount);
		Assert.Equal("USD", result.Currency);
	}

	[Fact]
	public void Addition_DifferentCurrency_Throws()
	{
		var act = () => Money.Create(10m, "USD") + Money.Create(5m, "EUR");

		Assert.Throws<InvalidOperationException>(act);
	}
}
