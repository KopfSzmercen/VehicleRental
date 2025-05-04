namespace VehicleRental.Rentals.Domain;

internal sealed record Money
{
    public Money(int amount, Currency currency)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");

        Amount = amount;
        Currency = currency;
    }

    public int Amount { get; }
    public Currency Currency { get; }

    public static Money operator +(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException("Cannot add money with different currencies.");

        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public static Money operator -(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException("Cannot subtract money with different currencies.");

        return new Money(left.Amount - right.Amount, left.Currency);
    }

    public static List<string> GetSupportedCurrencies()
    {
        return Enum.GetNames<Currency>().ToList();
    }
}