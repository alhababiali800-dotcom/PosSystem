using System;
using System.Collections.Generic;
using Pos.Domain.ValueObjects;

namespace Pos.Domain.Entities;

public class SaleReturnLine : BaseEntity
{
    public Guid SaleLineId { get; private set; }
    public Quantity Quantity { get; private set; }
    public Money RefundAmount { get; private set; }
    public Money ReturnCost { get; private set; }

    // EF Core parameterless ctor
    private SaleReturnLine() { }

    internal SaleReturnLine(Guid saleLineId, Quantity quantity, Money netUnitPrice, Money returnCost)
    {
        Id = Guid.NewGuid();
        SaleLineId = saleLineId;
        Quantity = quantity;
        RefundAmount = netUnitPrice * quantity.Value;
        ReturnCost = returnCost;
    }
}

public class SaleReturn : BaseEntity
{
    public Guid SaleId { get; private set; }

    private readonly List<SaleReturnLine> _lines = new();
    public IReadOnlyList<SaleReturnLine> Lines => _lines.AsReadOnly();

    // EF Core parameterless ctor
    private SaleReturn() { }

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
