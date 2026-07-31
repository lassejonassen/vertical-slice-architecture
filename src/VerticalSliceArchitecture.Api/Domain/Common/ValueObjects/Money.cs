namespace VerticalSliceArchitecture.Api.Domain.Common.ValueObjects;

public record Money(decimal Amount, string Currency)
{
	public static Money Zero(string currency = "USD") => new(0m, currency);

	public static Money Create(decimal amount, string currency = "USD")
	{
		if (amount < 0)
			throw new ArgumentException("Amount cannot be negative.", nameof(amount));

		return new Money(amount, currency);
	}

	public static Money operator +(Money a, Money b)
	{
		if (a.Currency != b.Currency)
			throw new InvalidOperationException("Cannot add money of different currencies.");

		return new Money(a.Amount + b.Amount, a.Currency);
	}
}