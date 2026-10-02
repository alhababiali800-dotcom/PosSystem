using System;
using System.Collections.Generic;
using Pos.Domain.ValueObjects;

namespace Pos.Domain.Entities;

public class SaleReturnLine
{
    public Guid Id { get; } = Guid.NewGuid();
    public Guid SaleLineId { get; }
    public Quantity Quantity { get; }
    public Money RefundAmount { get; }
    public Money ReturnCost { get; }

    internal SaleReturnLine(Guid saleLineId, Quantity quantity, Money netUnitPrice, Money returnCost)
    {
        SaleLineId = saleLineId;
        Quantity = quantity;
        RefundAmount = netUnitPrice * quantity.Value;
        ReturnCost = returnCost;
    }
}

public class SaleReturn
{
    public Guid Id { get; }
    public Guid SaleId { get; }
    
    private readonly List<SaleReturnLine> _lines = new();
    public IReadOnlyList<SaleReturnLine> Lines => _lines.AsReadOnly();

    public SaleReturn(Guid id, Guid saleId)
    {
        Id = id;
        SaleId = saleId;
    }

    public SaleReturnLine AddLine(SaleLine saleLine, Quantity quantityToReturn, Quantity alreadyReturned)
    {
        if (quantityToReturn.Value <= 0)
            throw new InvalidOperationException("Return quantity must be positive.");
            
        if (quantityToReturn.Value + alreadyReturned.Value > saleLine.Quantity.Value)
            throw new InvalidOperationException("Cannot exceed sold quantity minus already returned.");

        var line = new SaleReturnLine(saleLine.Id, quantityToReturn, saleLine.GetNetUnitPrice(), saleLine.UnitCostAtSale);
        _lines.Add(line);
        return line;
    }
}
