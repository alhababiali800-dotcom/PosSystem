using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Pos.Application.Interfaces;
using Pos.Domain.Entities;
using Pos.Domain.ValueObjects;

namespace Pos.Application.UseCases;

public class StockService : IStockService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStockRepository _stockRepo;
    private readonly IRepository<AuditLog> _auditRepo;

    public StockService(IUnitOfWork unitOfWork, IStockRepository stockRepo, IRepository<AuditLog> auditRepo)
    {
        _unitOfWork = unitOfWork;
        _stockRepo = stockRepo;
        _auditRepo = auditRepo;
    }

    public async Task AdjustStockAsync(Guid branchId, Guid productId, Quantity quantityDelta, string note, Guid userId, CancellationToken cancellationToken = default)
    {
        var balance = await _stockRepo.GetBalanceAsync(branchId, productId, cancellationToken);
        if (balance == null)
        {
            balance = new StockBalance(branchId, productId);
            await _stockRepo.AddBalanceAsync(balance, cancellationToken);
        }

        var movement = new StockMovement(branchId, productId, StockMovementType.Adjustment, quantityDelta, balance.AvgCost);
        balance.Apply(movement);

        await _stockRepo.AddMovementAsync(movement, cancellationToken);
        _stockRepo.UpdateBalance(balance);

        var audit = new AuditLog
        {
            UserId = userId,
            BranchId = branchId,
            Action = "AdjustStock",
            EntityType = "StockBalance",
            EntityId = $"{branchId}_{productId}",
            AfterJson = JsonSerializer.Serialize(new { ProductId = productId, Delta = quantityDelta.Value, NewQty = balance.Quantity.Value })
        };
        await _auditRepo.AddAsync(audit, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RebuildBalancesAsync(Guid branchId, Guid productId, CancellationToken cancellationToken = default)
    {
        var movements = await _stockRepo.GetMovementsAsync(branchId, productId, cancellationToken);
        var balance = await _stockRepo.GetBalanceAsync(branchId, productId, cancellationToken);
        
        if (balance == null)
        {
            balance = new StockBalance(branchId, productId);
            await _stockRepo.AddBalanceAsync(balance, cancellationToken);
        }
        else
        {
            // Reset balance
            balance = new StockBalance(branchId, productId);
            _stockRepo.UpdateBalance(balance);
        }

        foreach (var movement in movements)
        {
            balance.Apply(movement, allowNegativeStock: true);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
