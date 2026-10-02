using System;

namespace Pos.Domain.ValueObjects;

public readonly record struct Money(decimal Amount)
{
    public static Money Zero => new(0m);

    public Money Round(Currency currency)
    {
        return new Money(Math.Round(Amount, currency.MinorUnitDigits, MidpointRounding.AwayFromZero));
    }

    public static Money operator +(Money left, Money right) => new(left.Amount + right.Amount);
    public static Money operator -(Money left, Money right) => new(left.Amount - right.Amount);
    public static Money operator *(Money left, decimal multiplier) => new(left.Amount * multiplier);
    public static bool operator >(Money left, Money right) => left.Amount > right.Amount;
    public static bool operator >=(Money left, Money right) => left.Amount >= right.Amount;
    public static bool operator <(Money left, Money right) => left.Amount < right.Amount;
    public static bool operator <=(Money left, Money right) => left.Amount <= right.Amount;

    public override string ToString() => Amount.ToString("G");
}
