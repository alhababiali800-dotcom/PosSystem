using System;
using System.Threading;
using System.Threading.Tasks;
using Pos.Domain.Entities;
using Pos.Domain.ValueObjects;

namespace Pos.Application.UseCases;

public interface ISaleService
{
    Task<string> CompleteSaleAsync(Sale sale, Guid branchId, Guid deviceId, Guid userId, CancellationToken cancellationToken = default);
    Task<string> CompleteReturnAsync(SaleReturn saleReturn, Guid branchId, Guid deviceId, Guid userId, CancellationToken cancellationToken = default);
}

public interface IStockService
{
    Task AdjustStockAsync(Guid branchId, Guid productId, Quantity quantityDelta, string note, Guid userId, CancellationToken cancellationToken = default);
    Task RebuildBalancesAsync(Guid branchId, Guid productId, CancellationToken cancellationToken = default);
}

public interface IDocumentNumberGenerator
{
    Task<string> GenerateDocumentNumberAsync(Guid branchId, Guid deviceId, CancellationToken cancellationToken = default);
}

public interface IStockRepository
{
    Task<StockBalance?> GetBalanceAsync(Guid branchId, Guid productId, CancellationToken cancellationToken = default);
    Task AddBalanceAsync(StockBalance balance, CancellationToken cancellationToken = default);
    void UpdateBalance(StockBalance balance);
    Task AddMovementAsync(StockMovement movement, CancellationToken cancellationToken = default);
    Task<System.Collections.Generic.IReadOnlyList<StockMovement>> GetMovementsAsync(Guid branchId, Guid productId, CancellationToken cancellationToken = default);
}
