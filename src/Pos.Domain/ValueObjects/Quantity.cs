using System;

namespace Pos.Domain.ValueObjects;

public readonly record struct Quantity(decimal Value)
{
    public static Quantity Zero => new(0m);

    public Quantity Round()
    {
        return new Quantity(Math.Round(Value, 3, MidpointRounding.AwayFromZero));
    }

    public static Quantity operator +(Quantity left, Quantity right) => new(left.Value + right.Value);
    public static Quantity operator -(Quantity left, Quantity right) => new(left.Value - right.Value);
    public static bool operator >(Quantity left, Quantity right) => left.Value > right.Value;
    public static bool operator >=(Quantity left, Quantity right) => left.Value >= right.Value;
    public static bool operator <(Quantity left, Quantity right) => left.Value < right.Value;
    public static bool operator <=(Quantity left, Quantity right) => left.Value <= right.Value;
}
