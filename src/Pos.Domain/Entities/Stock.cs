using System;
using Pos.Domain.ValueObjects;

namespace Pos.Domain.Entities;

public enum StockMovementType
{
    Sale,
    SaleReturn,
    Purchase,
    PurchaseReturn,
    TransferOut,
    TransferIn,
    Adjustment,
    StocktakeSettlement,
    Opening
}

public class StockMovement
{
    public Guid Id { get; } = Guid.NewGuid();
    public Guid BranchId { get; }
    public Guid ProductId { get; }
    public StockMovementType Type { get; }
    public Quantity QuantityDelta { get; }
    public Money UnitCost { get; }

    public StockMovement(Guid branchId, Guid productId, StockMovementType type, Quantity quantityDelta, Money unitCost)
    {
        BranchId = branchId;
        ProductId = productId;
        Type = type;
        QuantityDelta = quantityDelta;
        UnitCost = unitCost;
    }
}

public class StockBalance
{
    public Guid BranchId { get; }
    public Guid ProductId { get; }
    public Quantity Quantity { get; private set; } = Quantity.Zero;
    public Money AvgCost { get; private set; } = Money.Zero;

    public StockBalance(Guid branchId, Guid productId)
    {
        BranchId = branchId;
        ProductId = productId;
    }

    public void Apply(StockMovement movement, bool allowNegativeStock = true)
    {
        var newQuantity = Quantity + movement.QuantityDelta;
        
        if (!allowNegativeStock && newQuantity.Value < 0)
        {
            throw new InvalidOperationException("Negative stock is not allowed.");
        }

        if (movement.QuantityDelta.Value > 0)
        {
            if (Quantity.Value <= 0)
            {
                AvgCost = movement.UnitCost;
            }
            else
            {
                var totalCurrentValue = Quantity.Value * AvgCost.Amount;
                var totalAddedValue = movement.QuantityDelta.Value * movement.UnitCost.Amount;
                var newTotalValue = totalCurrentValue + totalAddedValue;
                var newAvg = newTotalValue / newQuantity.Value;
                AvgCost = new Money(newAvg).Round(Currency.Default);
            }
        }
        else if (newQuantity.Value <= 0)
        {
            // If stock goes to zero or below, cost stays the same or we could reset it.
            // Leaving it as is.
        }

        Quantity = newQuantity;
    }
}
