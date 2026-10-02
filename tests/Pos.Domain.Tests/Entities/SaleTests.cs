using System;
using System.Linq;
using Pos.Domain.ValueObjects;
using Pos.Domain.Entities;
using Shouldly;
using Xunit;

namespace Pos.Domain.Tests.Entities;

public class SaleTests
{
    [Fact]
    public void Sale_Calculates_Totals_With_Exclusive_Tax()
    {
        var sale = new Sale(Guid.NewGuid());
        var taxRate = new TaxRate("VAT", 0.1m, IsInclusive: false);
        var currency = Currency.Default;
        
        sale.AddLine(Guid.NewGuid(), new Quantity(2m), new Money(100m), taxRate);
        
        // 2 * 100 = 200. Tax = 10% = 20. Total = 220.
        sale.CalculateTotals(currency);
        
        sale.Subtotal.Amount.ShouldBe(200m);
        sale.TaxTotal.Amount.ShouldBe(20m);
        sale.Total.Amount.ShouldBe(220m);
    }

    [Fact]
    public void Sale_Calculates_Totals_With_Inclusive_Tax()
    {
        var sale = new Sale(Guid.NewGuid());
        var taxRate = new TaxRate("VAT", 0.1m, IsInclusive: true);
        var currency = Currency.Default;
        
        sale.AddLine(Guid.NewGuid(), new Quantity(2m), new Money(110m), taxRate);
        
        // 2 * 110 = 220. Tax = 10% inside 220 => 220 / 1.1 * 0.1 = 20.
        sale.CalculateTotals(currency);
        
        sale.Subtotal.Amount.ShouldBe(200m);
        sale.TaxTotal.Amount.ShouldBe(20m);
        sale.Total.Amount.ShouldBe(220m);
    }

    [Fact]
    public void Sale_Calculates_Totals_With_Discounts()
    {
        var sale = new Sale(Guid.NewGuid());
        var taxRate = new TaxRate("NoTax", 0m, false);
        var currency = Currency.Default;
        
        var line = sale.AddLine(Guid.NewGuid(), new Quantity(2m), new Money(100m), taxRate);
        line.ApplyDiscount(new Money(20m)); // total line discount 20 (10 per unit). Net price = 90.
        
        sale.ApplyInvoiceDiscount(new Money(30m)); // overall discount 30.
        
        sale.CalculateTotals(currency);
        
        // Line subtotal = 2 * 90 = 180. Invoice discount = 30. Total = 150.
        sale.Total.Amount.ShouldBe(150m);
    }

    [Fact]
    public void Sale_Complete_Accepts_Exact_Mixed_Payments()
    {
        var sale = new Sale(Guid.NewGuid());
        sale.AddLine(Guid.NewGuid(), new Quantity(1m), new Money(100m), new TaxRate("NoTax", 0m, false));
        sale.CalculateTotals(Currency.Default);
        
        sale.AddPayment(new SalePayment(Guid.NewGuid(), PaymentMethod.Cash, new Money(40m)));
        sale.AddPayment(new SalePayment(Guid.NewGuid(), PaymentMethod.Card, new Money(60m)));
        
        sale.Complete(); // Should not throw
        
        sale.State.ShouldBe(SaleState.Completed);
    }

    [Fact]
    public void Sale_Complete_Rejects_Mismatched_Payments_T9()
    {
        var sale = new Sale(Guid.NewGuid());
        sale.AddLine(Guid.NewGuid(), new Quantity(1m), new Money(100m), new TaxRate("NoTax", 0m, false));
        sale.CalculateTotals(Currency.Default);
        
        sale.AddPayment(new SalePayment(Guid.NewGuid(), PaymentMethod.Cash, new Money(40m)));
        sale.AddPayment(new SalePayment(Guid.NewGuid(), PaymentMethod.Card, new Money(50m))); // Total 90 != 100
        
        Should.Throw<InvalidOperationException>(() => sale.Complete());
    }

    [Fact]
    public void Sale_Completed_Cannot_Change_T10()
    {
        var sale = new Sale(Guid.NewGuid());
        sale.AddLine(Guid.NewGuid(), new Quantity(1m), new Money(100m), new TaxRate("NoTax", 0m, false));
        sale.CalculateTotals(Currency.Default);
        sale.AddPayment(new SalePayment(Guid.NewGuid(), PaymentMethod.Cash, new Money(100m)));
        sale.Complete();
        
        Should.Throw<InvalidOperationException>(() => sale.AddLine(Guid.NewGuid(), new Quantity(1m), new Money(100m), new TaxRate("NoTax", 0m, false)));
        Should.Throw<InvalidOperationException>(() => sale.ApplyInvoiceDiscount(new Money(10m)));
        Should.Throw<InvalidOperationException>(() => sale.AddPayment(new SalePayment(Guid.NewGuid(), PaymentMethod.Cash, new Money(10m))));
    }
}
