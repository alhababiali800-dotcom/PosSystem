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
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid BranchId { get; private set; }
    public Guid ProductId { get; private set; }
    public StockMovementType Type { get; private set; }
    public Quantity QuantityDelta { get; private set; }
    public Money UnitCost { get; private set; }
    public DateTime OccurredAtUtc { get; private set; } = DateTime.UtcNow;

    // EF Core parameterless ctor
    private StockMovement() { }

    public StockMovement(Guid branchId, Guid productId, StockMovementType type, Quantity quantityDelta, Money unitCost)
    {
        BranchId = branchId;
        ProductId = productId;
        Type = type;
        QuantityDelta = quantityDelta;
        UnitCost = unitCost;
        OccurredAtUtc = DateTime.UtcNow;
    }

    /// <summary>Only for testing / data-import overrides.</summary>
    public void SetOccurredAt(DateTime utc) => OccurredAtUtc = utc;
}

public class StockBalance
{
    public Guid BranchId { get; private set; }
    public Guid ProductId { get; private set; }
    public Quantity Quantity { get; private set; } = Quantity.Zero;
    public Money AvgCost { get; private set; } = Money.Zero;

    // EF Core parameterless ctor
    private StockBalance() { }

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
            // If stock goes to zero or below, cost stays the same.
        }

        Quantity = newQuantity;
    }
}
