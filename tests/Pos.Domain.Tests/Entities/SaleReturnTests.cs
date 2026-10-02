using System;
using Pos.Domain.ValueObjects;
using Pos.Domain.Entities;
using Shouldly;
using Xunit;

namespace Pos.Domain.Tests.Entities;

public class SaleReturnTests
{
    [Fact]
    public void SaleReturn_Calculates_Refund_Correctly_T8()
    {
        var sale = new Sale(Guid.NewGuid());
        var tax = new TaxRate("NoTax", 0m, false);
        var line = sale.AddLine(Guid.NewGuid(), new Quantity(5m), new Money(100m), tax);
        line.ApplyDiscount(new Money(50m)); // 5 * 100 - 50 = 450. Net price = 90.
        
        var saleReturn = new SaleReturn(Guid.NewGuid(), sale.Id);
        // Returning 2 items, assuming 0 already returned
        var returnLine = saleReturn.AddLine(line, new Quantity(2m), Quantity.Zero);
        
        returnLine.RefundAmount.Amount.ShouldBe(180m);
    }

    [Fact]
    public void SaleReturn_Rejects_Exceeding_Sold_Quantity()
    {
        var sale = new Sale(Guid.NewGuid());
        var line = sale.AddLine(Guid.NewGuid(), new Quantity(5m), new Money(100m), new TaxRate("NoTax", 0m, false));
        
        var saleReturn = new SaleReturn(Guid.NewGuid(), sale.Id);
        
        Should.Throw<InvalidOperationException>(() => 
            saleReturn.AddLine(line, new Quantity(6m), Quantity.Zero));
            
        Should.Throw<InvalidOperationException>(() => 
            saleReturn.AddLine(line, new Quantity(2m), new Quantity(4m)));
    }
}
