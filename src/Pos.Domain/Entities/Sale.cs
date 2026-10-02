using System;
using System.Collections.Generic;
using System.Linq;
using Pos.Domain.ValueObjects;

namespace Pos.Domain.Entities;

public class SalePayment
{
    public Guid Id { get; }
    public PaymentMethod Method { get; }
    public Money Amount { get; }

    public SalePayment(Guid id, PaymentMethod method, Money amount)
    {
        Id = id;
        Method = method;
        Amount = amount;
    }
}

public class SaleLine
{
    public Guid Id { get; }
    public Guid ProductId { get; }
    public Quantity Quantity { get; private set; }
    public Money UnitPrice { get; private set; }
    public Money DiscountAmount { get; private set; }
    public TaxRate TaxRate { get; private set; }
    public Money UnitCostAtSale { get; internal set; }

    internal SaleLine(Guid productId, Quantity quantity, Money unitPrice, TaxRate taxRate)
    {
        Id = Guid.NewGuid();
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
        TaxRate = taxRate;
        DiscountAmount = Money.Zero;
    }

    public void ApplyDiscount(Money discount)
    {
        DiscountAmount = discount;
    }

    public Money GetNetUnitPrice()
    {
        if (Quantity.Value == 0) return UnitPrice;
        return new Money(UnitPrice.Amount - (DiscountAmount.Amount / Quantity.Value));
    }
}

public class Sale
{
    public Guid Id { get; }
    public SaleState State { get; private set; }
    
    private readonly List<SaleLine> _lines = new();
    public IReadOnlyList<SaleLine> Lines => _lines.AsReadOnly();
    
    private readonly List<SalePayment> _payments = new();
    public IReadOnlyList<SalePayment> Payments => _payments.AsReadOnly();

    public Money InvoiceDiscount { get; private set; } = Money.Zero;
    
    public Money Subtotal { get; private set; } = Money.Zero;
    public Money TaxTotal { get; private set; } = Money.Zero;
    public Money Total { get; private set; } = Money.Zero;

    public Sale(Guid id)
    {
        Id = id;
        State = SaleState.Draft;
    }

    public SaleLine AddLine(Guid productId, Quantity quantity, Money unitPrice, TaxRate taxRate)
    {
        EnsureDraft();
        var line = new SaleLine(productId, quantity, unitPrice, taxRate);
        _lines.Add(line);
        return line;
    }

    public void ApplyInvoiceDiscount(Money discount)
    {
        EnsureDraft();
        InvoiceDiscount = discount;
    }

    public void AddPayment(SalePayment payment)
    {
        EnsureDraft();
        _payments.Add(payment);
    }

    public void CalculateTotals(Currency currency)
    {
        EnsureDraft();
        
        decimal subtotalAmt = 0m;
        decimal taxTotalAmt = 0m;

        foreach (var line in _lines)
        {
            var netPrice = line.GetNetUnitPrice();
            var lineTotal = netPrice * line.Quantity.Value;

            if (line.TaxRate.IsInclusive)
            {
                var taxAmount = lineTotal.Amount - (lineTotal.Amount / (1m + line.TaxRate.Rate));
                var exTaxTotal = lineTotal.Amount - taxAmount;
                subtotalAmt += exTaxTotal;
                taxTotalAmt += taxAmount;
            }
            else
            {
                var taxAmount = lineTotal.Amount * line.TaxRate.Rate;
                subtotalAmt += lineTotal.Amount;
                taxTotalAmt += taxAmount;
            }
        }

        var subtotal = new Money(subtotalAmt).Round(currency);
        var taxTotal = new Money(taxTotalAmt).Round(currency);
        
        Subtotal = subtotal;
        TaxTotal = taxTotal;
        Total = new Money(subtotal.Amount + taxTotal.Amount - InvoiceDiscount.Amount).Round(currency);
    }

    public void Complete()
    {
        EnsureDraft();
        
        var paymentTotal = _payments.Sum(p => p.Amount.Amount);
        if (paymentTotal != Total.Amount)
        {
            throw new InvalidOperationException("Payments must equal the total amount.");
        }
        
        State = SaleState.Completed;
    }

    private void EnsureDraft()
    {
        if (State != SaleState.Draft)
        {
            throw new InvalidOperationException("Sale is completed and cannot be modified.");
        }
    }
}
