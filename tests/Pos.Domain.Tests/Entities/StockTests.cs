using System;
using Pos.Domain.ValueObjects;
using Pos.Domain.Entities;
using Shouldly;
using Xunit;

namespace Pos.Domain.Tests.Entities;

public class StockTests
{
    [Fact]
    public void StockBalance_Applies_WeightedAverageCost()
    {
        var balance = new StockBalance(Guid.NewGuid(), Guid.NewGuid());
        
        // Initial purchase of 10 @ 5.00
        var m1 = new StockMovement(balance.BranchId, balance.ProductId, StockMovementType.Purchase, new Quantity(10m), new Money(5.00m));
        balance.Apply(m1);
        
        balance.Quantity.Value.ShouldBe(10m);
        balance.AvgCost.Amount.ShouldBe(5.00m);
        
        // Second purchase of 10 @ 10.00
        var m2 = new StockMovement(balance.BranchId, balance.ProductId, StockMovementType.Purchase, new Quantity(10m), new Money(10.00m));
        balance.Apply(m2);
        
        balance.Quantity.Value.ShouldBe(20m);
        // (10 * 5 + 10 * 10) / 20 = 7.50
        balance.AvgCost.Amount.ShouldBe(7.50m);
    }

    [Fact]
    public void StockBalance_SaleReturn_Uses_OriginalCost()
    {
        var balance = new StockBalance(Guid.NewGuid(), Guid.NewGuid());
        balance.Apply(new StockMovement(balance.BranchId, balance.ProductId, StockMovementType.Purchase, new Quantity(10m), new Money(5.00m)));
        
        // Sale of 2 @ current average cost 5.00
        var mSale = new StockMovement(balance.BranchId, balance.ProductId, StockMovementType.Sale, new Quantity(-2m), balance.AvgCost);
        balance.Apply(mSale);
        
        // Now avg cost is still 5.00. Then purchase 10 @ 15.00
        balance.Apply(new StockMovement(balance.BranchId, balance.ProductId, StockMovementType.Purchase, new Quantity(10m), new Money(15.00m)));
        // Quantity = 18. Cost = (8 * 5 + 10 * 15) / 18 = 190 / 18 = 10.5555...
        
        // Return 1 of the sale, uses original cost 5.00
        var mReturn = new StockMovement(balance.BranchId, balance.ProductId, StockMovementType.SaleReturn, new Quantity(1m), new Money(5.00m));
        balance.Apply(mReturn);
        
        balance.Quantity.Value.ShouldBe(19m);
        // Cost = (18 * 10.56 + 5) / 19 = 195.08 / 19 = 10.267... -> 10.27
        balance.AvgCost.Amount.ShouldBe(10.27m);
    }

    [Fact]
    public void StockBalance_Rejects_NegativeStock_If_Disallowed()
    {
        var balance = new StockBalance(Guid.NewGuid(), Guid.NewGuid());
        var m1 = new StockMovement(balance.BranchId, balance.ProductId, StockMovementType.Sale, new Quantity(-5m), new Money(10m));
        
        Should.Throw<InvalidOperationException>(() => balance.Apply(m1, allowNegativeStock: false));
    }
}
