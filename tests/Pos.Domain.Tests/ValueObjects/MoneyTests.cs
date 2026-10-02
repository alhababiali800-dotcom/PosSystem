using Pos.Domain.ValueObjects;
using Shouldly;
using Xunit;

namespace Pos.Domain.Tests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Money_Rounding_FollowsAwayFromZero()
    {
        var currency = new Currency("BHD", "BD", 3);
        
        var money = new Money(1.0005m);
        var rounded = money.Round(currency);
        
        rounded.Amount.ShouldBe(1.001m);
    }

    [Fact]
    public void Money_Arithmetic_WorksCorrectly()
    {
        var m1 = new Money(10.5m);
        var m2 = new Money(5.25m);

        (m1 + m2).Amount.ShouldBe(15.75m);
        (m1 - m2).Amount.ShouldBe(5.25m);
        (m2 * 2m).Amount.ShouldBe(10.5m);
    }
}
