using Pos.Domain.ValueObjects;
using Shouldly;
using Xunit;

namespace Pos.Domain.Tests.ValueObjects;

public class QuantityTests
{
    [Fact]
    public void Quantity_Rounding_FollowsAwayFromZero_3Places()
    {
        var q = new Quantity(1.0005m);
        var rounded = q.Round();
        
        rounded.Value.ShouldBe(1.001m);
    }

    [Fact]
    public void Quantity_Arithmetic_WorksCorrectly()
    {
        var q1 = new Quantity(10.5m);
        var q2 = new Quantity(5.25m);

        (q1 + q2).Value.ShouldBe(15.75m);
        (q1 - q2).Value.ShouldBe(5.25m);
    }
}
