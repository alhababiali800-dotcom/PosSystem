namespace Pos.Domain.ValueObjects;

public record Currency(string Code, string Symbol, int MinorUnitDigits)
{
    public static readonly Currency Default = new("USD", "$", 2);
}
